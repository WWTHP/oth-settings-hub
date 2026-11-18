using System.Linq;

namespace SettingsHub.Tests
{
    public class TabBarTests
    {
        private static readonly float[] ThreeTabs = { 100f, 100f, 100f };
        private static readonly float[] FourTabs = { 100f, 100f, 100f, 50f };
        private static readonly float[] OneWide = { 50f, 500f, 50f };
        private static readonly int[] FirstThree = { 0, 1, 2 };
        private static readonly int[] RowA = { 0, 1 };
        private static readonly int[] RowB = { 2, 3 };
        private static readonly int[][] OneEach = { new[] { 0 }, new[] { 1 }, new[] { 2 } };
        private static readonly string[] DupNames = { "Fuel", "Yeti", "fuel", "Fuel", "Fuel (2)" };
        private static readonly string[] DupLabels = { "Fuel", "Yeti", "fuel (2)", "Fuel (3)", "Fuel (2) (2)" };

        [Fact]
        public void Width_ShortAndLongLabels_HasMinimumAndGrows()
        {
            Assert.Equal(TabBar.MinWidth, TabBar.Width("A"));
            Assert.Equal(TabBar.MinWidth, TabBar.Width(null));
            Assert.Equal(TabBar.Padding + 20 * TabBar.CharWidth, TabBar.Width(new string('x', 20)));
        }

        [Fact]
        public void Rows_FitInOneRow_SingleRow()
        {
            var rows = TabBar.Rows(ThreeTabs, 310f, 5f);

            Assert.Equal(FirstThree, Assert.Single(rows));
        }

        [Fact]
        public void Rows_Overflow_WrapsWithGapsCounted()
        {
            // 100+5+100+5+100 = 310 > 309: the third tab wraps.
            var rows = TabBar.Rows(FourTabs, 309f, 5f);

            Assert.Equal(2, rows.Count);
            Assert.Equal(RowA, rows[0]);
            Assert.Equal(RowB, rows[1]);
        }

        [Fact]
        public void Rows_TabWiderThanRow_GetsOwnRow()
        {
            var rows = TabBar.Rows(OneWide, 200f, 4f);

            Assert.Equal(OneEach, rows.Select(r => r.ToArray()));
        }

        [Fact]
        public void Rows_NoTabs_NoRows()
        {
            Assert.Empty(TabBar.Rows(System.Array.Empty<float>(), 100f, 4f));
        }

        [Fact]
        public void UniqueLabels_Duplicates_Numbered()
        {
            var labels = TabBar.UniqueLabels(DupNames);

            Assert.Equal(DupLabels, labels);
        }
    }
}
