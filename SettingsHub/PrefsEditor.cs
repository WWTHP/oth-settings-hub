using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SettingsHub
{
    /// <summary>
    /// Plain editor for mods that don't register a tab: draws a control per visible MelonPreferences
    /// entry (toggle, slider + text, key capture, text, enum cycle; read-only otherwise). Values are
    /// applied only when valid for the entry; edited categories are saved when the tab is left.
    /// </summary>
    internal static class PrefsEditor
    {
        private const float Pad = 12f, RowH = 26f, LabelW = 230f, FieldW = 90f, MaxText = 128;

        private static readonly Dictionary<MelonPreferences_Entry, string> Buffers = new();
        private static readonly HashSet<MelonPreferences_Category> Dirty = new();
        private static MelonPreferences_Entry _capturing;
        private static float _y;
        private static Dictionary<string, List<string>> _clashes = new();
        private static int _clashFrame = -1;

        public static IEnumerable<CategoryInfo> DescribeCategories() =>
            MelonPreferences.Categories
                .Where(c => c != null)
                .Select(c => new CategoryInfo(c.Identifier, c.DisplayName, c.IsHidden, c.Entries.Count(e => !e.IsHidden)));

        public static float DrawGroup(CategoryGroup group, float width)
        {
            RefreshClashes();
            _y = 4f;
            bool several = group.CategoryIds.Count > 1;
            foreach (var id in group.CategoryIds)
            {
                var cat = MelonPreferences.GetCategory(id);
                if (cat == null) continue;
                if (several) Section(string.IsNullOrWhiteSpace(cat.DisplayName) ? cat.Identifier : cat.DisplayName, width);
                foreach (var e in cat.Entries)
                    if (!e.IsHidden) DrawEntry(cat, e, width);
            }
            _y += 6f;
            Info("Some mods only pick up a change after a restart.", width);
            return _y + Pad;
        }

        public static void ResetGroup(CategoryGroup group)
        {
            foreach (var id in group.CategoryIds)
            {
                var cat = MelonPreferences.GetCategory(id);
                if (cat == null) continue;
                foreach (var e in cat.Entries)
                    if (!e.IsHidden) e.ResetToDefault();
                Dirty.Add(cat);
            }
            Buffers.Clear();
        }

        public static void SaveDirty()
        {
            foreach (var cat in Dirty)
            {
                try { cat.SaveToFile(false); }
                catch (Exception ex) { Mod.Log.Warning($"Saving [{cat.Identifier}] failed: {ex.Message}"); }
            }
            Dirty.Clear();
            Buffers.Clear();
        }

        // ------------------------------------------------------------------ rows

        private static void DrawEntry(MelonPreferences_Category cat, MelonPreferences_Entry e, float width)
        {
            var type = e.GetReflectedType();
            string label = string.IsNullOrWhiteSpace(e.DisplayName) ? e.Identifier : e.DisplayName;
            var labelRect = new Rect(Pad, _y, LabelW, RowH);
            var x = Pad + LabelW + 8f;
            var rest = width - x - Pad;

            if (type == typeof(bool))
            {
                bool v = (bool)e.BoxedValue;
                bool n = GUI.Toggle(new Rect(Pad, _y, width - 2 * Pad, RowH), v, " " + label);
                if (n != v) Set(cat, e, n);
            }
            else if (ValueText.IsNumeric(type))
            {
                GUI.Label(labelRect, label);
                DrawNumber(cat, e, type, x, rest);
            }
            else if (type == typeof(string) && KeyConflicts.IsKeySetting(e.Identifier, type) && IsInputSystemKey((string)e.BoxedValue))
            {
                GUI.Label(labelRect, label);
                DrawKey(cat, e, x);
            }
            else if (type == typeof(string))
            {
                GUI.Label(labelRect, label);
                var v = (string)e.BoxedValue ?? "";
                var n = GUI.TextField(new Rect(x, _y + 2f, rest, RowH - 4f), v, (int)MaxText);
                if (n != v && Valid(e, n)) Set(cat, e, n);
            }
            else if (type.IsEnum)
            {
                GUI.Label(labelRect, label);
                var names = Enum.GetNames(type);
                if (GUI.Button(new Rect(x, _y + 2f, Math.Min(rest, 200f), RowH - 4f), e.GetValueAsString()) && names.Length > 0)
                {
                    int i = Array.IndexOf(names, e.BoxedValue.ToString());
                    var next = Enum.Parse(type, names[(i + 1) % names.Length]);
                    if (Valid(e, next)) Set(cat, e, next);
                }
            }
            else
            {
                GUI.Label(labelRect, label);
                GUI.Label(new Rect(x, _y, rest, RowH), $"<color=#aaaaaa>{Trim(e.GetValueAsString(), 60)}  (edit in the .cfg)</color>");
            }
            _y += RowH;
            if (!string.IsNullOrWhiteSpace(e.Description))
                Info(Trim(e.Description, 160), width);
        }

        private static void DrawNumber(MelonPreferences_Category cat, MelonPreferences_Entry e, Type type, float x, float rest)
        {
            var range = e.Validator as IValueRange;
            object min = range?.MinValue, max = range?.MaxValue;
            float fieldX = x;
            if (ValueText.TrySliderRange(min, max, out double lo, out double hi) && ValueText.TryDouble(e.BoxedValue, out double cur))
            {
                float sliderW = rest - FieldW - 8f;
                float s = GUI.HorizontalSlider(new Rect(x, _y + 8f, sliderW, RowH - 8f), (float)cur, (float)lo, (float)hi);
                if (Math.Abs(s - cur) > 1e-6)
                {
                    var v = ValueText.FromSlider(s, type);
                    if (v != null && Valid(e, v)) { Set(cat, e, v); Buffers.Remove(e); }
                }
                fieldX = x + sliderW + 8f;
            }
            var shown = Buffers.TryGetValue(e, out var buf) ? buf : ValueText.Format(e.BoxedValue);
            bool ok = ValueText.TryParse(shown, type, min, max, out _);
            var text = GUI.TextField(new Rect(fieldX, _y + 2f, FieldW, RowH - 4f), shown, 24);
            if (!ok) GUI.Label(new Rect(fieldX - 14f, _y, 14f, RowH), "<color=#ff8080>!</color>");
            if (text == shown) return;
            Buffers[e] = text;
            if (ValueText.TryParse(text, type, min, max, out var parsed) && Valid(e, parsed)) Set(cat, e, parsed);
        }

        private static void DrawKey(MelonPreferences_Category cat, MelonPreferences_Entry e, float x)
        {
            bool capturing = ReferenceEquals(_capturing, e);
            var caption = capturing ? "Press a key… (Esc = cancel)" : (string)e.BoxedValue;
            if (GUI.Button(new Rect(x, _y + 2f, 200f, RowH - 4f), caption))
                _capturing = capturing ? null : e;
            if (GUI.Button(new Rect(x + 208f, _y + 2f, 60f, RowH - 4f), "Clear") && Valid(e, ""))
                Set(cat, e, "");
            if (_clashes.TryGetValue(KeySetting.Id(cat.Identifier, e.Identifier), out var others))
                GUI.Label(new Rect(x + 276f, _y, 200f, RowH), $"<color=#ffb060>also: {Trim(string.Join(", ", others), 40)}</color>");
        }

        // ------------------------------------------------------------------ key capture

        /// <summary>
        /// While a key setting waits for a key: the next key pressed is stored (Escape cancels).
        /// Returns true on the frame that consumed a key, so it doesn't also act as a hotkey.
        /// </summary>
        public static bool UpdateCapture()
        {
            if (_capturing == null) return false;
            var kb = Keyboard.current;
            if (kb == null) return false;
            if (kb[Key.Escape].wasPressedThisFrame) { _capturing = null; return true; }
            foreach (Key k in Enum.GetValues<Key>())
            {
                if (k == Key.None || k == Key.Escape) continue;
                bool down;
                try { down = kb[k].wasPressedThisFrame; }
                catch { continue; } // enum values without a control on this keyboard
                if (!down) continue;
                var e = _capturing;
                _capturing = null;
                var name = k.ToString();
                if (Valid(e, name)) Set(e.Category, e, name);
                Mod.Log.Msg($"{e.Category.Identifier}.{e.Identifier} = {name}");
                return true;
            }
            return false;
        }

        public static void CancelCapture() => _capturing = null;

        private static bool IsInputSystemKey(string value) =>
            string.IsNullOrWhiteSpace(value) || Enum.TryParse(value, true, out Key _);

        // ------------------------------------------------------------------ helpers

        private static void RefreshClashes()
        {
            if (_clashFrame == Time.frameCount) return;
            _clashFrame = Time.frameCount;
            var keys = new List<KeySetting>();
            foreach (var c in MelonPreferences.Categories)
            {
                if (c == null) continue;
                foreach (var e in c.Entries)
                    if (!e.IsHidden && KeyConflicts.IsKeySetting(e.Identifier, e.GetReflectedType()))
                        keys.Add(new KeySetting(c.Identifier, e.Identifier, e.BoxedValue as string));
            }
            _clashes = KeyConflicts.Find(keys);
        }

        private static bool Valid(MelonPreferences_Entry e, object value) =>
            e.Validator == null || e.Validator.IsValid(value);

        private static void Set(MelonPreferences_Category cat, MelonPreferences_Entry e, object value)
        {
            try
            {
                e.BoxedValue = value;
                Dirty.Add(cat);
            }
            catch (Exception ex) { Mod.Log.Warning($"{cat.Identifier}.{e.Identifier}: {ex.Message}"); }
        }

        private static void Section(string title, float width)
        {
            _y += 6f;
            GUI.Label(new Rect(Pad, _y, width - 2 * Pad, RowH), $"<b><color=#9fd3ff>{title}</color></b>");
            _y += RowH;
        }

        private static void Info(string text, float width)
        {
            float h = text.Length > 80 ? RowH + 16f : RowH - 6f;
            GUI.Label(new Rect(Pad + 16f, _y - 4f, width - 2 * Pad - 16f, h), $"<size=12><color=#aaaaaa>{text}</color></size>");
            _y += h - 2f;
        }

        private static string Trim(string s, int max) =>
            s == null ? "" : s.Length <= max ? s : string.Concat(s.AsSpan(0, max - 1), "…");
    }
}
