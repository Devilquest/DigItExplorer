using DigItExplorer.Core.Export;

namespace DigItExplorer.Tests.Export;

/// <summary>Verifies row interleaving and dimension calculations in <see cref="FrameStrip"/>.</summary>
public class FrameStripTests
{
    [Fact]
    public void One_frame_comes_back_unchanged()
        => Assert.Equal([1, 2, 3, 4], FrameStrip.Compose([[1, 2, 3, 4]], 2, 2, 1));

    /// <summary>Verifies that sprite frames interleave correctly row by row.</summary>
    [Fact]
    public void Frames_are_interleaved_row_by_row()
    {
        // Two 2×2 frames:  A = 1 2   B = 5 6
        //                      3 4       7 8
        var strip = FrameStrip.Compose([[1, 2, 3, 4], [5, 6, 7, 8]], 2, 2, bytesPerPixel: 1);

        Assert.Equal(
        [
            1, 2, 5, 6,
            3, 4, 7, 8,
        ], strip);
    }

    [Fact]
    public void A_frame_taller_than_it_is_wide_keeps_its_shape()
    {
        // Two 1×3 frames, stacked values -> a 2×3 strip
        var strip = FrameStrip.Compose([[1, 2, 3], [7, 8, 9]], 1, 3, bytesPerPixel: 1);

        Assert.Equal(
        [
            1, 7,
            2, 8,
            3, 9,
        ], strip);
    }

    [Fact]
    public void Rgba_pixels_stay_whole()
    {
        // Two 1×1 RGBA frames: opaque red, half-transparent blue
        var strip = FrameStrip.Compose([[255, 0, 0, 255], [0, 0, 255, 128]], 1, 1, bytesPerPixel: 4);

        Assert.Equal([255, 0, 0, 255, 0, 0, 255, 128], strip);
    }

    [Fact]
    public void The_strip_is_as_wide_as_its_frames_together()
    {
        var strip = FrameStrip.Compose([[0, 0, 0, 0], [0, 0, 0, 0], [0, 0, 0, 0]], 2, 2, 1);
        Assert.Equal(3 * 2 * 2, strip.Length);
    }

    [Fact]
    public void No_frames_at_all_is_rejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() => FrameStrip.Compose([], 2, 2, 1));

    [Fact]
    public void A_frame_too_small_for_the_stated_size_is_rejected()
        => Assert.Throws<ArgumentException>(() => FrameStrip.Compose([[1, 2, 3, 4], [1, 2]], 2, 2, 1));
}
