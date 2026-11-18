using System;
using System.Collections.Generic;

namespace SettingsHub
{
    /// <summary>
    /// One tab another mod registered (see snippets/generic/HubTab.cs.txt for the mod side).
    /// The registration is a <c>Dictionary&lt;string, object&gt;</c> holding only BCL types, so mods
    /// never reference the hub's assembly.
    /// </summary>
    internal sealed class TabSpec
    {
        public const int MaxNameLength = 24;
        public const int MaxCategories = 16;
        public const int DefaultOrder = 100;

        public string Name { get; private set; }
        public string Owner { get; private set; }
        public Func<float, float> Draw { get; private set; }
        public IReadOnlyList<string> Categories { get; private set; }
        public int Order { get; private set; }
        public Action Opened { get; private set; }
        public Action Closed { get; private set; }
        public Action Reset { get; private set; }

        /// <summary>
        /// Builds a tab from a registration. Required: "name" (1-24 chars, no control characters)
        /// and "draw" (Func&lt;float, float&gt;: content width → content height). Optional: "owner"
        /// (string), "categories" (string[] of MelonPreferences category ids, max 16), "order"
        /// (int), "opened" / "closed" / "reset" (Action). Present keys of the wrong type, or
        /// unknown keys, reject the whole registration with a reason.
        /// </summary>
        public static TabSpec TryParse(IDictionary<string, object> reg, out string error)
        {
            error = null;
            if (reg == null) { error = "registration is null"; return null; }
            var spec = new TabSpec { Order = DefaultOrder, Categories = Array.Empty<string>() };
            foreach (var kv in reg)
            {
                switch (kv.Key)
                {
                    case "name":
                        if (kv.Value is not string name || !IsTabName(name)) { error = "name must be 1-24 printable characters"; return null; }
                        spec.Name = name.Trim();
                        break;
                    case "owner":
                        if (kv.Value is not string owner || owner.Length > 64) { error = "owner must be a string of at most 64 characters"; return null; }
                        spec.Owner = owner;
                        break;
                    case "draw":
                        if (kv.Value is not Func<float, float> draw) { error = "draw must be a Func<float, float>"; return null; }
                        spec.Draw = draw;
                        break;
                    case "categories":
                        if (kv.Value is not string[] cats || cats.Length > MaxCategories) { error = "categories must be a string[] of at most 16 ids"; return null; }
                        foreach (var c in cats)
                            if (string.IsNullOrWhiteSpace(c)) { error = "categories must not contain empty ids"; return null; }
                        spec.Categories = (string[])cats.Clone();
                        break;
                    case "order":
                        if (kv.Value is not int order) { error = "order must be an int"; return null; }
                        spec.Order = order;
                        break;
                    case "opened":
                        if (!TryAction(kv.Value, out var opened)) { error = "opened must be an Action"; return null; }
                        spec.Opened = opened;
                        break;
                    case "closed":
                        if (!TryAction(kv.Value, out var closed)) { error = "closed must be an Action"; return null; }
                        spec.Closed = closed;
                        break;
                    case "reset":
                        if (!TryAction(kv.Value, out var reset)) { error = "reset must be an Action"; return null; }
                        spec.Reset = reset;
                        break;
                    default:
                        error = $"unknown key '{kv.Key}'";
                        return null;
                }
            }
            if (spec.Name == null) { error = "name is missing"; return null; }
            if (spec.Draw == null) { error = "draw is missing"; return null; }
            return spec;
        }

        private static bool TryAction(object value, out Action action)
        {
            action = value as Action;
            return value == null || action != null;
        }

        private static bool IsTabName(string s)
        {
            var t = s.Trim();
            if (t.Length == 0 || t.Length > MaxNameLength) return false;
            foreach (var c in t)
                if (char.IsControl(c)) return false;
            return true;
        }
    }
}
