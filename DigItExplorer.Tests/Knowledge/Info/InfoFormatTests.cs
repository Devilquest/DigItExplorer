using DigItExplorer.Core.Knowledge.Info;

namespace DigItExplorer.Tests;

/// <summary>Verifies byte count formatting and rounding rules in <see cref="InfoFormat.Bytes"/>.</summary>
public class InfoFormatTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(768, "768 B")]      // a VGA palette, the smallest backing file a level has
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(12648, "12.4 KB")]
    public void Bytes_states_the_exact_count_below_a_kibibyte_and_rounds_above_it(int count, string expected)
        => Assert.Equal(expected, InfoFormat.Bytes(count));

    [Theory]
    [InlineData(0.0, "0.00 s")]
    [InlineData(0.37, "0.37 s")]
    [InlineData(59.99, "59.99 s")]
    [InlineData(60.0, "1:00 min")]
    [InlineData(160.0, "2:40 min")]
    public void Duration_formats_seconds_or_minutes_with_units(double seconds, string expected)
        => Assert.Equal(expected, InfoFormat.Duration(seconds));

    [Theory]
    [InlineData(11025, "11,025 Hz")]
    [InlineData(44100, "44,100 Hz")]
    public void SampleRate_formats_hertz_with_thousands_separator(int hz, string expected)
        => Assert.Equal(expected, InfoFormat.SampleRate(hz));
}

