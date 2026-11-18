using System;
using System.Globalization;

namespace SettingsHub
{
    /// <summary>
    /// Text ↔ number for the plain editor's text fields. Parsing is strict: invariant culture,
    /// finite, within the entry's range if it has one. Anything else is rejected (the field keeps
    /// the text and shows it as invalid), never clamped.
    /// </summary>
    internal static class ValueText
    {
        public static bool TryParse(string text, Type type, object min, object max, out object value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text) || type == null) return false;
            text = text.Trim();
            const NumberStyles F = NumberStyles.Float;
            const NumberStyles I = NumberStyles.Integer;
            var inv = CultureInfo.InvariantCulture;
            if (type == typeof(float))
            {
                if (!float.TryParse(text, F, inv, out var f) || !float.IsFinite(f)) return false;
                if (min is float lo && f < lo || max is float hi && f > hi) return false;
                value = f;
            }
            else if (type == typeof(double))
            {
                if (!double.TryParse(text, F, inv, out var d) || !double.IsFinite(d)) return false;
                if (min is double lo && d < lo || max is double hi && d > hi) return false;
                value = d;
            }
            else if (type == typeof(int))
            {
                if (!int.TryParse(text, I, inv, out var n)) return false;
                if (min is int lo && n < lo || max is int hi && n > hi) return false;
                value = n;
            }
            else if (type == typeof(long))
            {
                if (!long.TryParse(text, I, inv, out var n)) return false;
                if (min is long lo && n < lo || max is long hi && n > hi) return false;
                value = n;
            }
            else return false;
            return true;
        }

        public static bool IsNumeric(Type t) =>
            t == typeof(float) || t == typeof(double) || t == typeof(int) || t == typeof(long);

        /// <summary>Invariant text for a number (floats keep up to 4 decimals).</summary>
        public static string Format(object value) => value switch
        {
            float f => f.ToString("0.####", CultureInfo.InvariantCulture),
            double d => d.ToString("0.####", CultureInfo.InvariantCulture),
            IFormattable x => x.ToString(null, CultureInfo.InvariantCulture),
            null => "",
            _ => value.ToString(),
        };

        /// <summary>
        /// Converts a number to double for a slider; false if not numeric or the range isn't
        /// finite and ordered (no slider then, only the text field).
        /// </summary>
        public static bool TrySliderRange(object min, object max, out double lo, out double hi)
        {
            lo = hi = 0;
            if (!TryDouble(min, out lo) || !TryDouble(max, out hi)) return false;
            return double.IsFinite(lo) && double.IsFinite(hi) && hi > lo;
        }

        public static bool TryDouble(object v, out double d)
        {
            switch (v)
            {
                case float f: d = f; return true;
                case double x: d = x; return true;
                case int n: d = n; return true;
                case long l: d = l; return true;
                default: d = 0; return false;
            }
        }

        /// <summary>Slider position → entry type (integers rounded). Null for non-numeric types.</summary>
        public static object FromSlider(double v, Type type)
        {
            if (type == typeof(float)) return (float)v;
            if (type == typeof(double)) return v;
            if (type == typeof(int)) return (int)Math.Round(v);
            if (type == typeof(long)) return (long)Math.Round(v);
            return null;
        }
    }
}
