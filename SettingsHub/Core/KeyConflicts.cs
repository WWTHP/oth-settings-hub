using System;
using System.Collections.Generic;
using System.Linq;

namespace SettingsHub
{
    /// <summary>A key setting: which tab/setting it belongs to and the key name it holds.</summary>
    internal readonly struct KeySetting
    {
        public KeySetting(string owner, string setting, string key)
        {
            Owner = owner;
            Setting = setting;
            Key = key;
        }

        public string Owner { get; }
        public string Setting { get; }
        public string Key { get; }

        /// <summary>Lookup key for <see cref="KeyConflicts.Find"/>'s result.</summary>
        public static string Id(string owner, string setting) => owner + "\u0001" + setting;
    }

    /// <summary>
    /// Key settings are string entries whose id ends in "Key" (MenuKey, SiphonKey…). Finds keys
    /// bound more than once, so the editor can warn next to each clash.
    /// </summary>
    internal static class KeyConflicts
    {
        public static bool IsKeySetting(string entryId, Type type) =>
            type == typeof(string) && entryId != null && entryId.EndsWith("Key", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// For each setting (by <see cref="KeySetting.Id"/>) whose key is also bound elsewhere, the other bindings as
        /// "Owner Setting" texts. Empty keys and "None" never clash. Key names compare case-insensitively.
        /// </summary>
        public static Dictionary<string, List<string>> Find(IEnumerable<KeySetting> settings)
        {
            var bound = settings
                .Where(s => !string.IsNullOrWhiteSpace(s.Key) && !s.Key.Trim().Equals("None", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var result = new Dictionary<string, List<string>>();
            foreach (var g in bound.GroupBy(s => s.Key.Trim(), StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            {
                var members = g.ToList();
                for (int i = 0; i < members.Count; i++)
                {
                    var others = new List<string>();
                    for (int j = 0; j < members.Count; j++)
                        if (j != i) others.Add($"{members[j].Owner} {members[j].Setting}");
                    result[KeySetting.Id(members[i].Owner, members[i].Setting)] = others;
                }
            }
            return result;
        }
    }
}
