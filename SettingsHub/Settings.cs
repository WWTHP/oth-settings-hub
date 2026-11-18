using System;
using MelonLoader;
using UnityEngine.InputSystem;

namespace SettingsHub
{
    /// <summary>User settings, stored in UserData/MelonPreferences.cfg [SettingsHub].</summary>
    internal static class Settings
    {
        internal static MelonPreferences_Category Cat;
        internal static MelonPreferences_Entry<bool> Enabled;
        internal static MelonPreferences_Entry<string> MenuKey;
        internal static MelonPreferences_Entry<bool> ShowOtherMods;

        public static void Init()
        {
            Cat = MelonPreferences.CreateCategory("SettingsHub", "Settings hub");
            Enabled = Cat.CreateEntry("Enabled", true, "Enabled",
                "Turn the settings hub on or off (off: each mod uses its own menu key again).");
            // Registry: docs/games/over-the-hill/README.md. F2/F3 are OTHNight's, F6-F11 our mods'.
            MenuKey = Cat.CreateEntry("MenuKey", "F5", "Menu key",
                "Key that opens the settings hub (Unity Input System key name).");
            ShowOtherMods = Cat.CreateEntry("ShowOtherMods", true, "Show other mods",
                "Also show tabs for mods that don't register with the hub (plain settings editor).");
        }

        public static void Save() => Cat.SaveToFile(false);

        public static bool Pressed(MelonPreferences_Entry<string> entry)
        {
            var kb = Keyboard.current;
            if (kb == null || !Enum.TryParse(entry.Value, true, out Key key) || key == Key.None) return false;
            return kb[key].wasPressedThisFrame;
        }
    }
}
