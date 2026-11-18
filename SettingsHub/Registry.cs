using System;
using System.Collections.Generic;

namespace SettingsHub
{
    /// <summary>
    /// The process-wide meeting point with other mods. No mod references another assembly: both
    /// sides only use <see cref="AppDomain"/> data slots holding BCL types.
    /// <list type="bullet">
    /// <item><c>WWTHP.SettingsHub.Hub.v1</c> = hub version string while the hub is enabled. Mods
    /// check it to skip their own menu key.</item>
    /// <item><c>WWTHP.SettingsHub.Tabs.v1</c> = <c>List&lt;Dictionary&lt;string, object&gt;&gt;</c>, one
    /// entry per tab (keys: see <see cref="TabSpec.TryParse"/>). Whoever comes first creates it.</item>
    /// </list>
    /// Main thread only. Mod side: snippets/generic/HubTab.cs.txt.
    /// </summary>
    internal static class Registry
    {
        public const string HubKey = "WWTHP.SettingsHub.Hub.v1";
        public const string TabsKey = "WWTHP.SettingsHub.Tabs.v1";

        private static readonly List<TabSpec> Parsed = new();
        private static readonly HashSet<object> Rejected = new(ReferenceEqualityComparer.Instance);
        private static int _seen;

        public static void Announce(bool on) =>
            AppDomain.CurrentDomain.SetData(HubKey, on ? Mod.Version : null);

        /// <summary>Valid tabs registered so far, in registration order. Re-reads only new entries.</summary>
        public static IReadOnlyList<TabSpec> Tabs()
        {
            if (AppDomain.CurrentDomain.GetData(TabsKey) is not List<Dictionary<string, object>> list) return Parsed;
            for (; _seen < list.Count; _seen++)
            {
                var reg = list[_seen];
                if (reg == null || Rejected.Contains(reg)) continue;
                var spec = TabSpec.TryParse(reg, out var error);
                if (spec != null)
                {
                    Parsed.Add(spec);
                    Mod.Log.Msg($"Tab '{spec.Name}' registered by {spec.Owner ?? "a mod"}" +
                                (spec.Categories.Count > 0 ? $" (covers {string.Join(", ", spec.Categories)})" : ""));
                }
                else
                {
                    Rejected.Add(reg);
                    Mod.Log.Warning($"Ignored a tab registration: {error}");
                }
            }
            return Parsed;
        }
    }
}
