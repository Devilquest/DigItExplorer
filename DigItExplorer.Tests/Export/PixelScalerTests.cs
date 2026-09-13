using DigItExplorer.Core.Export;

namespace DigItExplorer.Tests.Export;

/// <summary>Verifies integer pixel magnification, orientation preservation, and channel interleaving in <see cref="PixelScaler"/>.</summary>
public class PixelScalerTests
{
    [Fact]
    public void Factor_one_hands_back_the_same_buffer()
    {
        var pixels = new byte[] { 1, 2, 3, 4 };
        Assert.Same(pixels, PixelScaler.Magnify(pixels, 2, 2, 1, 1));
    }

    [Fact]
    public void Each_index_becomes_a_square_block()
    {
        // 2×2:  1 2
        //       3 4
        var scaled = PixelScaler.Magnify([1, 2, 3, 4], 2, 2, bytesPerPixel: 1, factor: 2);

        Assert.Equal(
        [
            1, 1, 2, 2,
            1, 1, 2, 2,
            3, 3, 4, 4,
            3, 3, 4, 4,
        ], scaled);
    }

    /// <summary>Verifies that non-square pixel buffers preserve rectangular aspect ratio when scaled.</summary>
    [Fact]
    public void A_non_square_image_keeps_its_orientation()
    {
        // 3×1: 1 2 3  ->  6×2
        var scaled = PixelScaler.Magnify([1, 2, 3], 3, 1, bytesPerPixel: 1, factor: 2);

        Assert.Equal(
        [
            1, 1, 2, 2, 3, 3,
            1, 1, 2, 2, 3, 3,
        ], scaled);
    }

    /// <summary>Verifies that multi-byte RGB color channels remain grouped across magnified pixels.</summary>
    [Fact]
    public void Rgb_channels_travel_together()
    {
        // 2×1 RGB: red, green
        var scaled = PixelScaler.Magnify([255, 0, 0, 0, 255, 0], 2, 1, bytesPerPixel: 3, factor: 2);

        Assert.Equal(
        [
            255, 0, 0, 255, 0, 0, 0, 255, 0, 0, 255, 0,
            255, 0, 0, 255, 0, 0, 0, 255, 0, 0, 255, 0,
        ], scaled);
    }

    [Fact]
    public void Rgba_channels_travel_together()
    {
        // 1×1 RGBA, half-transparent blue
        var scaled = PixelScaler.Magnify([0, 0, 255, 128], 1, 1, bytesPerPixel: 4, factor: 3);

        Assert.Equal(3 * 3 * 4, scaled.Length);
        for (int p = 0; p < 9; p++)
            Assert.Equal<byte[]>([0, 0, 255, 128], scaled[(p * 4)..(p * 4 + 4)]);
    }

    [Fact]
    public void Odd_factors_work_too()
    {
        var scaled = PixelScaler.Magnify([7, 9], 2, 1, bytesPerPixel: 1, factor: 3);
        Assert.Equal([7, 7, 7, 9, 9, 9, 7, 7, 7, 9, 9, 9, 7, 7, 7, 9, 9, 9], scaled);
    }

    [Theory]
    [InlineData(0, 1, 1, 2)]  // zero width
    [InlineData(1, 0, 1, 2)]  // zero height
    [InlineData(1, 1, 0, 2)]  // zero bytes per pixel
    [InlineData(1, 1, 1, 0)]  // zero factor
    [InlineData(1, 1, 1, -1)] // shrinking is not what this is for
    public void Nonsense_arguments_are_rejected(int width, int height, int bytesPerPixel, int factor)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => PixelScaler.Magnify([0, 0, 0, 0], width, height, bytesPerPixel, factor));

    [Fact]
    public void A_buffer_too_small_for_its_stated_size_is_rejected()
        => Assert.Throws<ArgumentException>(() => PixelScaler.Magnify([1, 2, 3], 2, 2, 1, 2));
}
