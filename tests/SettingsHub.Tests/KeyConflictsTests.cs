using System;

namespace SettingsHub.Tests
{
    public class KeyConflictsTests
    {
        private static readonly string[] OtherMenu = { "Other MenuKey" };
        private static readonly string[] HubMenu = { "SettingsHub MenuKey" };
        private static readonly string[] AandC = { "A K", "C K" };

        [Theory]
        [InlineData("MenuKey", true)]
        [InlineData("siphonkey", true)]
        [InlineData("MenuKeyModifier", false)]
        [InlineData("Keywords", false)]
        [InlineData(null, false)]
        public void IsKeySetting_StringEntry_ByIdSuffix(string id, bool expected)
        {
            Assert.Equal(expected, KeyConflicts.IsKeySetting(id, typeof(string)));
        }

        [Fact]
        public void IsKeySetting_NonString_False()
        {
            Assert.False(KeyConflicts.IsKeySetting("MenuKey", typeof(int)));
        }

        [Fact]
        public void Find_SameKeyTwice_ReportsEachOther()
        {
            var clashes = KeyConflicts.Find(new[]
            {
                new KeySetting("SettingsHub", "MenuKey", "F5"),
                new KeySetting("Other", "MenuKey", " f5 "),
                new KeySetting("Fuel", "MenuKey", "F10"),
            });

            Assert.Equal(2, clashes.Count);
            Assert.Equal(OtherMenu, clashes[KeySetting.Id("SettingsHub", "MenuKey")]);
            Assert.Equal(HubMenu, clashes[KeySetting.Id("Other", "MenuKey")]);
        }

        [Fact]
        public void Find_ThreeWayClash_ListsBothOthers()
        {
            var clashes = KeyConflicts.Find(new[]
            {
                new KeySetting("A", "K", "F1"), new KeySetting("B", "K", "F1"), new KeySetting("C", "K", "F1"),
            });

            Assert.Equal(AandC, clashes[KeySetting.Id("B", "K")]);
        }

        [Fact]
        public void Find_EmptyNullOrNone_NeverClash()
        {
            var clashes = KeyConflicts.Find(new[]
            {
                new KeySetting("A", "K", ""), new KeySetting("B", "K", ""),
                new KeySetting("C", "K", null), new KeySetting("D", "K", null),
                new KeySetting("E", "K", "None"), new KeySetting("F", "K", "none"),
            });

            Assert.Empty(clashes);
        }

        [Fact]
        public void Id_DifferentOwnerSettingSplits_Differ()
        {
            Assert.NotEqual(KeySetting.Id("AB", "C"), KeySetting.Id("A", "BC"));
        }
    }
}
