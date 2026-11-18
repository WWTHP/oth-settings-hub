using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;

namespace SettingsHub
{
    /// <summary>
    /// The hub window (IMGUI): one tab per mod. Registered tabs are drawn by their mod; every other
    /// MelonPreferences category gets a plain editor (<see cref="PrefsEditor"/>). Opening and
    /// closing unlocks/restores the cursor like the mods' own menus do.
    /// </summary>
    internal static class Hub
    {
        public static bool IsOpen { get; private set; }

        private const float W = 640f, Pad = 12f, RowH = 26f, TabH = 26f, TabGap = 4f, HeaderH = 34f, FooterH = 44f;

        private sealed class Tab
        {
            public string Label;
            public TabSpec Spec;
            public CategoryGroup Group;
            public string Error;
            public float ContentH = 400f;
            public Vector2 Scroll;
        }

        private static readonly List<Tab> TabsShown = new();
        private static int _active, _registeredSeen = -1;
        private static string _activeLabel;
        private static CursorLockMode _prevLock;
        private static bool _prevVisible;

        public static void Update()
        {
            if (PrefsEditor.UpdateCapture()) return; // a key press being captured isn't a hub key
            if (Settings.Pressed(Settings.MenuKey)) Toggle();
            if (IsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public static void Toggle()
        {
            if (!IsOpen)
            {
                IsOpen = true;
                _prevLock = Cursor.lockState;
                _prevVisible = Cursor.visible;
                Rebuild();
                Select(Math.Clamp(_active, 0, Math.Max(0, TabsShown.Count - 1)), opening: true);
            }
            else
            {
                Leave(CurrentTab());
                IsOpen = false;
                PrefsEditor.CancelCapture();
                Cursor.lockState = _prevLock;
                Cursor.visible = _prevVisible;
            }
        }

        /// <summary>Called when the hub is switched off while open.</summary>
        public static void Close()
        {
            if (IsOpen) Toggle();
        }

        private static Tab CurrentTab() => _active >= 0 && _active < TabsShown.Count ? TabsShown[_active] : null;

        private static void Rebuild()
        {
            var registered = Registry.Tabs();
            _registeredSeen = registered.Count;
            var ordered = registered.OrderBy(s => s.Order).ToList(); // stable: ties keep registration order
            var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in ordered) claimed.UnionWith(s.Categories);

            var groups = CategoryGroups.Build(PrefsEditor.DescribeCategories(), claimed)
                .Where(g => Settings.ShowOtherMods.Value || g.CategoryIds.Contains(Settings.Cat.Identifier))
                .ToList();

            var names = new List<string>();
            foreach (var s in ordered) names.Add(s.Name);
            foreach (var g in groups) names.Add(g.Title);
            var labels = TabBar.UniqueLabels(names);
            var old = new Dictionary<string, Tab>();
            foreach (var t in TabsShown) old[t.Label] = t;
            TabsShown.Clear();
            for (int i = 0; i < labels.Count; i++)
            {
                var tab = old.TryGetValue(labels[i], out var keep) ? keep : new Tab { Label = labels[i] };
                tab.Spec = i < ordered.Count ? ordered[i] : null;
                tab.Group = i < ordered.Count ? null : groups[i - ordered.Count];
                TabsShown.Add(tab);
            }
            int again = TabsShown.FindIndex(t => t.Label == _activeLabel);
            if (again >= 0) _active = again;
        }

        private static void Select(int index, bool opening = false)
        {
            if (!opening && index == _active) return;
            if (!opening) Leave(CurrentTab());
            _active = index;
            var tab = CurrentTab();
            _activeLabel = tab?.Label;
            if (tab?.Spec?.Opened != null) Call(tab, tab.Spec.Opened, "opened");
        }

        private static void Leave(Tab tab)
        {
            if (tab == null) return;
            if (tab.Spec?.Closed != null) Call(tab, tab.Spec.Closed, "closed");
            PrefsEditor.SaveDirty();
        }

        private static void Call(Tab tab, Action action, string what)
        {
            try { action(); }
            catch (Exception ex) { Fail(tab, what, ex); }
        }

        private static void Fail(Tab tab, string what, Exception ex)
        {
            if (tab.Error == null)
                Mod.Log.Warning($"Tab '{tab.Label}' ({tab.Spec?.Owner ?? "plain"}) threw in {what}: {ex}");
            tab.Error = $"{what} failed: {ex.GetType().Name}: {ex.Message}";
        }

        public static void Draw()
        {
            if (!IsOpen) return;
            if (Registry.Tabs().Count != _registeredSeen) Rebuild();

            float scale = Mathf.Max(1f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            try { DrawWindow(scale); }
            finally { GUI.matrix = Matrix4x4.identity; }
        }

        private static void DrawWindow(float scale)
        {
            float screenW = Screen.width / scale, screenH = Screen.height / scale;
            var win = new Rect(screenW - W - 40f, 40f, W, screenH - 80f);
            GUI.Box(win, "");
            GUI.Box(win, "");
            GUI.Label(new Rect(win.x + Pad, win.y + 8f, W - 2 * Pad, RowH),
                $"<b>Mod settings</b>   ({Settings.MenuKey.Value} to close)");

            // Tab bar.
            float y = win.y + HeaderH;
            var widths = TabsShown.Select(t => TabBar.Width(t.Label)).ToList();
            foreach (var row in TabBar.Rows(widths, W - 2 * Pad, TabGap))
            {
                float x = win.x + Pad;
                foreach (int i in row)
                {
                    var label = i == _active ? $"<b>{TabsShown[i].Label}</b>" : TabsShown[i].Label;
                    if (GUI.Toggle(new Rect(x, y, widths[i], TabH), i == _active, label, GUI.skin.button) && i != _active)
                        Select(i);
                    x += widths[i] + TabGap;
                }
                y += TabH + TabGap;
            }
            y += 4f;

            var tab = CurrentTab();
            var view = new Rect(win.x, y, W, win.yMax - FooterH - y);
            if (tab == null)
                GUI.Label(new Rect(win.x + Pad, y, W - 2 * Pad, RowH), "No mod settings found.");
            else
                DrawContent(tab, view);

            // Footer.
            float by = win.yMax - 36f;
            bool canReset = tab != null && (tab.Group != null || tab.Spec?.Reset != null);
            if (canReset && GUI.Button(new Rect(win.x + Pad, by, 140f, 26f), "Reset defaults"))
            {
                if (tab.Group != null) PrefsEditor.ResetGroup(tab.Group);
                else Call(tab, tab.Spec.Reset, "reset");
            }
            GUI.Label(new Rect(win.x + Pad + 150f, by + 3f, 330f, 22f),
                "<size=12><color=#aaaaaa>Saved when you switch tab or close.</color></size>");
            if (GUI.Button(new Rect(win.xMax - Pad - 110f, by, 110f, 26f), "Close")) Toggle();
        }

        private static void DrawContent(Tab tab, Rect view)
        {
            float innerW = W - 24f;
            if (tab.Error != null)
            {
                GUI.Label(new Rect(view.x + Pad, view.y, W - 2 * Pad, RowH * 2),
                    $"<color=#ff8080>This tab stopped working: {tab.Error}</color>");
                if (GUI.Button(new Rect(view.x + Pad, view.y + RowH * 2, 120f, 24f), "Try again")) tab.Error = null;
                return;
            }
            tab.Scroll = GUI.BeginScrollView(view, tab.Scroll, new Rect(0f, 0f, innerW, Mathf.Max(tab.ContentH, 1f)));
            try
            {
                float h = tab.Spec != null ? tab.Spec.Draw(innerW) : PrefsEditor.DrawGroup(tab.Group, innerW);
                if (float.IsFinite(h) && h > 0f) tab.ContentH = Mathf.Min(h, 100000f);
            }
            catch (Exception ex) { Fail(tab, "draw", ex); }
            finally { GUI.EndScrollView(); }
        }
    }
}
