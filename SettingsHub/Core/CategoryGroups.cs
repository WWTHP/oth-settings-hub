using System;
using System.Collections.Generic;
using System.Linq;

namespace SettingsHub
{
    /// <summary>What the hub needs to know about a MelonPreferences category.</summary>
    internal readonly struct CategoryInfo
    {
        public CategoryInfo(string id, string displayName, bool hidden, int visibleEntries)
        {
            Id = id;
            DisplayName = displayName;
            Hidden = hidden;
            VisibleEntries = visibleEntries;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public bool Hidden { get; }
        public int VisibleEntries { get; }
    }

    /// <summary>A plain-editor tab: one or more categories shown together.</summary>
    internal sealed class CategoryGroup
    {
        public CategoryGroup(string title, IReadOnlyList<string> categoryIds)
        {
            Title = title;
            CategoryIds = categoryIds;
        }

        public string Title { get; }
        public IReadOnlyList<string> CategoryIds { get; }
    }

    /// <summary>
    /// Groups the categories no registered tab claims into plain-editor tabs. A mod with several
    /// categories usually prefixes them (<c>OTHNight</c>, <c>OTHNight_Keybinds</c>), so categories
    /// are grouped by the part of the id before the first '_'.
    /// </summary>
    internal static class CategoryGroups
    {
        public static List<CategoryGroup> Build(IEnumerable<CategoryInfo> categories, ICollection<string> claimed)
        {
            var groups = new Dictionary<string, List<CategoryInfo>>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();
            foreach (var c in categories)
            {
                if (c.Hidden || c.VisibleEntries <= 0 || string.IsNullOrWhiteSpace(c.Id)) continue;
                if (claimed != null && claimed.Contains(c.Id)) continue;
                var key = Prefix(c.Id);
                if (!groups.TryGetValue(key, out var list))
                {
                    groups[key] = list = new List<CategoryInfo>();
                    order.Add(key);
                }
                list.Add(c);
            }
            return order
                .Select(key => new CategoryGroup(Title(key, groups[key]), groups[key].Select(c => c.Id).ToList()))
                .OrderBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string Prefix(string id)
        {
            int i = id.IndexOf('_');
            return i > 0 ? id.Substring(0, i) : id;
        }

        private static string Title(string key, List<CategoryInfo> members)
        {
            var root = members.FirstOrDefault(c => string.Equals(c.Id, key, StringComparison.OrdinalIgnoreCase));
            var name = root.Id != null ? root.DisplayName : null;
            return string.IsNullOrWhiteSpace(name) ? key : name.Trim();
        }
    }
}
