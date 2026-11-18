using System;
using System.Collections.Generic;

namespace SettingsHub.Tests
{
    public class TabSpecTests
    {
        private static readonly string[] BlankId = { "A", "" };
        private static readonly string[] NullId = { "A", null };
        private static readonly string[] FuelCats = { "FuelManagement" };

        private static readonly Func<float, float> Draw = w => 10f;

        private static Dictionary<string, object> Minimal() => new() { ["name"] = "Fuel", ["draw"] = Draw };

        [Fact]
        public void TryParse_MinimalRegistration_ReturnsSpecWithDefaults()
        {
            var spec = TabSpec.TryParse(Minimal(), out var error);

            Assert.Null(error);
            Assert.Equal("Fuel", spec.Name);
            Assert.Same(Draw, spec.Draw);
            Assert.Equal(TabSpec.DefaultOrder, spec.Order);
            Assert.Empty(spec.Categories);
            Assert.Null(spec.Owner);
            Assert.Null(spec.Opened);
            Assert.Null(spec.Closed);
            Assert.Null(spec.Reset);
        }

        [Fact]
        public void TryParse_FullRegistration_ReturnsAllFields()
        {
            Action opened = () => { }, closed = () => { }, reset = () => { };
            var cats = (string[])FuelCats.Clone();
            var reg = Minimal();
            reg["name"] = "  Fuel  ";
            reg["owner"] = "FuelManagement 0.3.0";
            reg["categories"] = cats;
            reg["order"] = 5;
            reg["opened"] = opened;
            reg["closed"] = closed;
            reg["reset"] = reset;

            var spec = TabSpec.TryParse(reg, out var error);

            Assert.Null(error);
            Assert.Equal("Fuel", spec.Name);
            Assert.Equal("FuelManagement 0.3.0", spec.Owner);
            Assert.Equal(cats, spec.Categories);
            Assert.NotSame(cats, spec.Categories);
            Assert.Equal(5, spec.Order);
            Assert.Same(opened, spec.Opened);
            Assert.Same(closed, spec.Closed);
            Assert.Same(reset, spec.Reset);
        }

        [Fact]
        public void TryParse_NullCallbacks_AreAllowed()
        {
            var reg = Minimal();
            reg["opened"] = null;

            Assert.NotNull(TabSpec.TryParse(reg, out _));
        }

        [Fact]
        public void TryParse_Null_IsRejected()
        {
            Assert.Null(TabSpec.TryParse(null, out var error));
            Assert.NotNull(error);
        }

        public static IEnumerable<object[]> BadRegistrations()
        {
            object[] Case(string key, object value) => new object[] { key, value };
            yield return Case("name", null);
            yield return Case("name", "");
            yield return Case("name", "   ");
            yield return Case("name", new string('x', 25));
            yield return Case("name", "Tab\nTwo");
            yield return Case("name", 42);
            yield return Case("draw", null);
            yield return Case("draw", (Func<float>)(() => 1f));
            yield return Case("draw", (Action<float>)(_ => { }));
            yield return Case("owner", 1);
            yield return Case("owner", new string('o', 65));
            yield return Case("categories", "FuelManagement");
            yield return Case("categories", BlankId);
            yield return Case("categories", NullId);
            yield return Case("categories", new string[17]);
            yield return Case("order", 1.5f);
            yield return Case("order", "1");
            yield return Case("opened", "x");
            yield return Case("closed", (Action<int>)(_ => { }));
            yield return Case("reset", 3);
            yield return Case("colour", "red");
        }

        [Theory]
        [MemberData(nameof(BadRegistrations))]
        public void TryParse_BadValue_IsRejectedWithReason(string key, object value)
        {
            var reg = Minimal();
            reg[key] = value;

            var spec = TabSpec.TryParse(reg, out var error);

            Assert.Null(spec);
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Theory]
        [InlineData("name")]
        [InlineData("draw")]
        public void TryParse_RequiredKeyMissing_IsRejected(string key)
        {
            var reg = Minimal();
            reg.Remove(key);

            Assert.Null(TabSpec.TryParse(reg, out var error));
            Assert.Contains(key, error);
        }
    }
}
