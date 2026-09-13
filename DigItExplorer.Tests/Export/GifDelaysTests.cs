using DigItExplorer.Core.Export;

namespace DigItExplorer.Tests.Export;

/// <summary>Verifies fractional centisecond dithering and duration drift prevention in <see cref="GifDelays"/>.</summary>
public class GifDelaysTests
{
    private const double GameMsPerFrame = 3000.0 / 70; // 42.86 ms, three VGA vsyncs

    /// <summary>Verifies that dithered centisecond delays alternate between 4 and 5 to match game frame rates.</summary>
    [Fact]
    public void The_games_rate_alternates_four_and_five()
        => Assert.Equal([4, 5, 4, 4, 4, 5, 4, 4], GifDelays.Centiseconds(8, GameMsPerFrame));

    /// <summary>Verifies that cumulative animation duration never drifts from the exact fractional time.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(37)]
    [InlineData(500)]
    public void The_running_total_never_drifts(int frameCount)
    {
        int total = GifDelays.Centiseconds(frameCount, GameMsPerFrame).Sum();
        double exact = frameCount * GameMsPerFrame / 10;

        Assert.True(Math.Abs(total - exact) <= 0.5,
            $"{frameCount} frames: {total} hundredths against an exact {exact:0.00}");
    }

    [Fact]
    public void A_rate_that_lands_on_a_whole_hundredth_needs_no_dithering()
        => Assert.Equal([5, 5, 5, 5], GifDelays.Centiseconds(4, 50));

    /// <summary>Verifies that sub-centisecond frame delays clamp to a minimum of 1 centisecond.</summary>
    [Fact]
    public void No_frame_is_ever_given_no_time()
        => Assert.All(GifDelays.Centiseconds(20, 1), delay => Assert.True(delay >= 1));

    [Fact]
    public void No_frames_means_no_delays()
        => Assert.Empty(GifDelays.Centiseconds(0, GameMsPerFrame));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_rate_of_nothing_is_rejected(double msPerFrame)
        => Assert.Throws<ArgumentOutOfRangeException>(() => GifDelays.Centiseconds(4, msPerFrame));
}
