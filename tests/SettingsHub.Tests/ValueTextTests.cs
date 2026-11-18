using System;

namespace SettingsHub.Tests
{
    public class ValueTextTests
    {
        [Theory]
        [InlineData("1.5", 1.5f)]
        [InlineData(" 2 ", 2f)]
        [InlineData("-0.25", -0.25f)]
        [InlineData("1e2", 100f)]
        public void TryParse_ValidFloat_ReturnsValue(string text, float expected)
        {
            Assert.True(ValueText.TryParse(text, typeof(float), null, null, out var v));
            Assert.Equal(expected, (float)v);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(null)]
        [InlineData("1,5")]
        [InlineData("abc")]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        [InlineData("1e40")]
        public void TryParse_InvalidFloat_Rejected(string text)
        {
            Assert.False(ValueText.TryParse(text, typeof(float), null, null, out _));
        }

        [Theory]
        [InlineData("0", true)]
        [InlineData("10", true)]
        [InlineData("-0.1", false)]
        [InlineData("10.01", false)]
        public void TryParse_FloatOutsideRange_RejectedNotClamped(string text, bool ok)
        {
            Assert.Equal(ok, ValueText.TryParse(text, typeof(float), 0f, 10f, out _));
        }

        [Theory]
        [InlineData("3", true)]
        [InlineData("0", false)]
        [InlineData("11", false)]
        [InlineData("2.5", false)]
        [InlineData("99999999999", false)]
        public void TryParse_Int_StrictWithRange(string text, bool ok)
        {
            Assert.Equal(ok, ValueText.TryParse(text, typeof(int), 1, 10, out var v));
            if (ok) Assert.Equal(3, v);
        }

        [Fact]
        public void TryParse_DoubleAndLong_WithRanges()
        {
            Assert.True(ValueText.TryParse("0.5", typeof(double), 0d, 1d, out var d));
            Assert.Equal(0.5d, d);
            Assert.False(ValueText.TryParse("2", typeof(double), 0d, 1d, out _));
            Assert.False(ValueText.TryParse("NaN", typeof(double), null, null, out _));
            Assert.True(ValueText.TryParse("5000000000", typeof(long), null, null, out var l));
            Assert.Equal(5000000000L, l);
            Assert.False(ValueText.TryParse("7", typeof(long), 8L, 9L, out _));
            Assert.False(ValueText.TryParse("x", typeof(long), null, null, out _));
        }

        [Fact]
        public void TryParse_UnsupportedType_Rejected()
        {
            Assert.False(ValueText.TryParse("1", typeof(string), null, null, out _));
            Assert.False(ValueText.TryParse("1", null, null, null, out _));
        }

        [Fact]
        public void TryParse_RangeOfOtherType_Ignored()
        {
            // A validator typed differently from the entry never matches; the value is not range-checked.
            Assert.True(ValueText.TryParse("50", typeof(float), 0, 10, out _));
        }

        [Theory]
        [InlineData(typeof(float), true)]
        [InlineData(typeof(double), true)]
        [InlineData(typeof(int), true)]
        [InlineData(typeof(long), true)]
        [InlineData(typeof(string), false)]
        [InlineData(typeof(bool), false)]
        public void IsNumeric_Type_ReturnsExpected(Type t, bool expected)
        {
            Assert.Equal(expected, ValueText.IsNumeric(t));
        }

        [Fact]
        public void Format_Values_InvariantAndShort()
        {
            Assert.Equal("0.1235", ValueText.Format(0.123456f));
            Assert.Equal("2", ValueText.Format(2f));
            Assert.Equal("1.5", ValueText.Format(1.5d));
            Assert.Equal("42", ValueText.Format(42));
            Assert.Equal("", ValueText.Format(null));
            Assert.Equal("x", ValueText.Format("x"));
        }

        [Fact]
        public void TrySliderRange_FiniteOrderedRange_ReturnsBounds()
        {
            Assert.True(ValueText.TrySliderRange(0.5f, 2f, out var lo, out var hi));
            Assert.Equal(0.5, lo);
            Assert.Equal(2, hi);
            Assert.True(ValueText.TrySliderRange(1, 10L, out _, out _));
        }

        [Theory]
        [InlineData(null, 1f)]
        [InlineData(1f, null)]
        [InlineData(2f, 1f)]
        [InlineData(1f, 1f)]
        [InlineData(float.NegativeInfinity, 1f)]
        [InlineData("0", 1f)]
        public void TrySliderRange_MissingOrBadRange_ReturnsFalse(object min, object max)
        {
            Assert.False(ValueText.TrySliderRange(min, max, out _, out _));
        }

        [Fact]
        public void FromSlider_ConvertsToEntryType()
        {
            Assert.Equal(1.5f, ValueText.FromSlider(1.5, typeof(float)));
            Assert.Equal(1.5d, ValueText.FromSlider(1.5, typeof(double)));
            Assert.Equal(2, ValueText.FromSlider(1.6, typeof(int)));
            Assert.Equal(2L, ValueText.FromSlider(2.4, typeof(long)));
            Assert.Null(ValueText.FromSlider(1, typeof(string)));
        }
    }
}
