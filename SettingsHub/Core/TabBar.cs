using System;
using System.Collections.Generic;

namespace SettingsHub
{
    /// <summary>Tab bar layout: tabs flow left to right and wrap into more rows when needed.</summary>
    internal static class TabBar
    {
        public const float CharWidth = 8f, Padding = 20f, MinWidth = 60f;

        /// <summary>Estimated button width for a tab label (IMGUI default font, ~8 px per char).</summary>
        public static float Width(string label) =>
            Math.Max(MinWidth, Padding + CharWidth * (label?.Length ?? 0));

        /// <summary>
        /// Splits tabs into rows no wider than <paramref name="maxWidth"/>. A tab wider than the
        /// row gets a row of its own. Returns the index ranges as rows of indices.
        /// </summary>
        public static List<List<int>> Rows(IReadOnlyList<float> widths, float maxWidth, float gap)
        {
            var rows = new List<List<int>>();
            var row = new List<int>();
            float x = 0f;
            for (int i = 0; i < widths.Count; i++)
            {
                float need = (row.Count > 0 ? gap : 0f) + widths[i];
                if (row.Count > 0 && x + need > maxWidth)
                {
                    rows.Add(row);
                    row = new List<int>();
                    x = 0f;
                    need = widths[i];
                }
                row.Add(i);
                x += need;
            }
            if (row.Count > 0) rows.Add(row);
            return rows;
        }

        /// <summary>
        /// Distinct labels: a repeated name gets " (2)", " (3)"… in order of appearance.
        /// </summary>
        public static List<string> UniqueLabels(IEnumerable<string> names)
        {
            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (var n in names)
            {
                var label = n;
                if (!taken.Add(label))
                {
                    seen.TryGetValue(n, out int k);
                    do { k = Math.Max(k, 1) + 1; label = $"{n} ({k})"; } while (!taken.Add(label));
                    seen[n] = k;
                }
                result.Add(label);
            }
            return result;
        }
    }
}
