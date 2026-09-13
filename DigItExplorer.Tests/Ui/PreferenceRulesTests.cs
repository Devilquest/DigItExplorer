using DigItExplorer.Core.Ui;

namespace DigItExplorer.Tests;

/// <summary>Verifies volume clamping, export scale constraints, and enum parsing fallbacks in <see cref="PreferenceRules"/>.</summary>
public sealed class PreferenceRulesTests
{
    [Theory]
    [InlineData(0.5, 0.5)] // in range, unchanged
    [InlineData(0.0, 0.0)] // zero is a valid volume, not clamped away
    [InlineData(1.0, 1.0)]
    [InlineData(7.0, 1.0)] // above range
    [InlineData(-2.0, 0.0)] // below range
    [InlineData(double.NaN, 1.0)] // Math.Clamp alone would pass NaN through unchanged
    [InlineData(double.PositiveInfinity, 1.0)]
    public void ClampVolume_keeps_zero_but_rejects_out_of_range_and_non_finite(double raw, double expected)
        => Assert.Equal(expected, PreferenceRules.ClampVolume(raw));

    [Theory]
    [InlineData(0.5, 0.5)]
    [InlineData(1.0, 1.0)]
    [InlineData(2.0, 1.0)] // above range, clamped down rather than rejected
    [InlineData(0.0, 1.0)] // zero would make unmute silent forever
    [InlineData(-1.0, 1.0)]
    [InlineData(double.NaN, 1.0)]
    public void ClampLastAudibleVolume_never_returns_zero(double raw, double expected)
        => Assert.Equal(expected, PreferenceRules.ClampLastAudibleVolume(raw));

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    [InlineData(3, 1)] // not one of the three magnifications
    [InlineData(0, 1)]
    [InlineData(-4, 1)]
    public void ClampExportScale_accepts_only_1_2_or_4(int raw, int expected)
        => Assert.Equal(expected, PreferenceRules.ClampExportScale(raw));

    private enum Sample { First, Second }

    [Fact]
    public void ParseEnumOrDefault_accepts_a_real_members_own_name()
        => Assert.Equal(Sample.Second, PreferenceRules.ParseEnumOrDefault("Second", Sample.First));

    [Fact]
    public void ParseEnumOrDefault_is_case_insensitive()
        => Assert.Equal(Sample.Second, PreferenceRules.ParseEnumOrDefault("second", Sample.First));

    [Fact]
    public void ParseEnumOrDefault_falls_back_on_a_name_nothing_declares()
        => Assert.Equal(Sample.First, PreferenceRules.ParseEnumOrDefault("Nonsense", Sample.First));

    [Fact]
    public void ParseEnumOrDefault_falls_back_on_a_numeric_string_that_parses_but_names_nothing()
        => Assert.Equal(Sample.First, PreferenceRules.ParseEnumOrDefault("99", Sample.First));

    [Fact]
    public void ParseEnumOrDefault_falls_back_on_null_or_empty()
    {
        Assert.Equal(Sample.First, PreferenceRules.ParseEnumOrDefault(null, Sample.First));
        Assert.Equal(Sample.First, PreferenceRules.ParseEnumOrDefault("", Sample.First));
    }
}
