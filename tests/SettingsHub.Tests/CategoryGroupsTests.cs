using System.Collections.Generic;
using System.Linq;

namespace SettingsHub.Tests
{
    public class CategoryGroupsTests
    {
        private static readonly string[] NightIds = { "OTHNight_Keybinds", "OTHNight" };
        private static readonly string[] KeptOnly = { "Kept" };
        private static readonly string[] Sorted = { "Alpha", "beta", "Charlie" };

        private static CategoryInfo Cat(string id, string display = null, bool hidden = false, int entries = 3) =>
            new(id, display, hidden, entries);

        [Fact]
        public void Build_PrefixedCategories_GroupedUnderRootDisplayName()
        {
            var cats = new[] { Cat("OTHNight_Keybinds", "Real Nights and Lights keybinds"), Cat("OTHNight", "Real Nights and Lights") };

            var groups = CategoryGroups.Build(cats, new HashSet<string>());

            var g = Assert.Single(groups);
            Assert.Equal("Real Nights and Lights", g.Title);
            Assert.Equal(NightIds, g.CategoryIds);
        }

        [Fact]
        public void Build_NoRootCategory_UsesPrefixAsTitle()
        {
            var groups = CategoryGroups.Build(new[] { Cat("Foo_A", "A"), Cat("Foo_B", "B") }, null);

            Assert.Equal("Foo", Assert.Single(groups).Title);
        }

        [Fact]
        public void Build_RootWithoutDisplayName_UsesId()
        {
            var groups = CategoryGroups.Build(new[] { Cat("WombatGD", "  ") }, null);

            Assert.Equal("WombatGD", Assert.Single(groups).Title);
        }

        [Fact]
        public void Build_ClaimedHiddenEmptyOrBlank_AreSkipped()
        {
            var cats = new[]
            {
                Cat("FuelManagement", "Fuel"),
                Cat("Secret", hidden: true),
                Cat("Empty", entries: 0),
                Cat(" "),
                Cat("Kept", "Kept"),
            };
            var claimed = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "fuelmanagement" };

            var groups = CategoryGroups.Build(cats, claimed);

            Assert.Equal(KeptOnly, groups.Select(g => g.Title));
        }

        [Fact]
        public void Build_SeveralMods_SortedByTitleIgnoringCase()
        {
            var groups = CategoryGroups.Build(new[] { Cat("b", "beta"), Cat("C", "Charlie"), Cat("A", "Alpha") }, null);

            Assert.Equal(Sorted, groups.Select(g => g.Title));
        }

        [Theory]
        [InlineData("OTHNight_Keybinds", "OTHNight")]
        [InlineData("OTHNight", "OTHNight")]
        [InlineData("_Leading", "_Leading")]
        [InlineData("A_B_C", "A")]
        public void Prefix_Id_ReturnsPartBeforeFirstUnderscore(string id, string expected)
        {
            Assert.Equal(expected, CategoryGroups.Prefix(id));
        }
    }
}
