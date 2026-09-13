using DigItExplorer.Core.Export;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests.Export;

/// <summary>Verifies GIF89a streaming encoder output, LZW compression, frame disposal, and round-trip decoding in <see cref="GifEncoder"/>.</summary>
public class GifEncoderTests
{
    private const double GameMsPerFrame = 3000.0 / 70; // 42.86 ms, three VGA vsyncs

    /// <summary>A palette where no two indices share a color, so a decoded index is traceable.</summary>
    private static VgaPalette Palette()
    {
        var raw = new byte[768];
        for (int i = 0; i < 256; i++)
        {
            raw[i * 3 + 0] = (byte)(i % 64);
            raw[i * 3 + 1] = (byte)((i / 4) % 64);
            raw[i * 3 + 2] = (byte)((i * 5) % 64);
        }
        return VgaPalette.From6Bit(raw);
    }

    private static byte[] Encode(IReadOnlyList<byte[]> frames, int width, int height,
        double msPerFrame = GameMsPerFrame, bool loop = true, byte transparent = 0)
    {
        using var stream = new MemoryStream();
        GifEncoder.Write(stream, frames, width, height, Palette(), msPerFrame, transparent, loop);
        return stream.ToArray();
    }

    private static byte[] Ramp(int length, int seed)
    {
        var pixels = new byte[length];
        for (int i = 0; i < length; i++) pixels[i] = (byte)((i * 7 + seed) & 0xFF);
        return pixels;
    }

    [Fact]
    public void The_file_is_a_gif89a_and_is_terminated()
    {
        var bytes = Encode([Ramp(64, 0)], 8, 8);

        Assert.Equal("GIF89a"u8.ToArray(), bytes[..6]);
        Assert.Equal(0x3B, bytes[^1]);
    }

    [Fact]
    public void The_canvas_and_palette_are_the_ones_given()
    {
        var gif = GifReader.Read(Encode([Ramp(48 * 20, 0)], 48, 20));

        Assert.Equal(48, gif.Width);
        Assert.Equal(20, gif.Height);
        Assert.Equal(Palette().Rgb[..768].ToArray(), gif.Palette);
    }

    [Theory]
    [InlineData(true, 0)]  // forever
    [InlineData(false, 1)] // once
    public void Looping_is_declared_in_the_netscape_block(bool loop, int expected)
        => Assert.Equal(expected, GifReader.Read(Encode([Ramp(64, 0)], 8, 8, loop: loop)).LoopCount);

    [Fact]
    public void Every_frame_declares_transparency_and_restores_the_background()
    {
        var gif = GifReader.Read(Encode([Ramp(64, 0), Ramp(64, 1), Ramp(64, 2)], 8, 8, transparent: 0));

        Assert.Equal(3, gif.Frames.Count);
        foreach (var frame in gif.Frames)
        {
            // Disposal 2 is what stops a sprite smearing across its own path: without it each frame is
            // drawn over whatever the last one left, and every transparent pixel shows it through.
            Assert.Equal(2, frame.Disposal);
            Assert.Equal(0, frame.TransparentIndex);
        }
    }

    /// <summary>Verifies that frame delays preserve target playback speed across animation loops.</summary>
    [Fact]
    public void Delays_hold_the_games_rate_across_the_animation()
    {
        var gif = GifReader.Read(Encode([.. Enumerable.Range(0, 8).Select(i => Ramp(64, i))], 8, 8));

        Assert.Equal([4, 5, 4, 4, 4, 5, 4, 4], gif.Frames.Select(f => f.DelayCentiseconds));
        Assert.NotEqual(8 * 4, gif.Frames.Sum(f => f.DelayCentiseconds));
    }

    [Fact]
    public void One_frame_survives_the_round_trip()
    {
        var pixels = Ramp(32 * 24, 0);
        var gif = GifReader.Read(Encode([pixels], 32, 24));

        Assert.Equal(pixels, gif.Frames[0].Pixels);
    }

    [Fact]
    public void Every_frame_of_an_animation_survives_the_round_trip()
    {
        var frames = Enumerable.Range(0, 6).Select(i => Ramp(40 * 30, i * 11)).ToList();
        var gif = GifReader.Read(Encode(frames, 40, 30));

        Assert.Equal(6, gif.Frames.Count);
        for (int i = 0; i < frames.Count; i++) Assert.Equal(frames[i], gif.Frames[i].Pixels);
    }

    /// <summary>Tests LZW encoding round-trip when dictionary resets occur repeatedly on incompressible data.</summary>
    [Fact]
    public void A_frame_that_fills_the_dictionary_repeatedly_survives_the_round_trip()
    {
        var random = new Random(20260808);
        var pixels = new byte[320 * 200];
        random.NextBytes(pixels);

        var gif = GifReader.Read(Encode([pixels], 320, 200));

        Assert.Equal(pixels, gif.Frames[0].Pixels);
    }

    /// <summary>Verifies round-trip encoding of solid single-color frames.</summary>
    [Fact]
    public void A_frame_of_one_color_survives_the_round_trip()
    {
        var pixels = new byte[100 * 100];
        Array.Fill(pixels, (byte)7);

        var gif = GifReader.Read(Encode([pixels], 100, 100));

        Assert.Equal(pixels, gif.Frames[0].Pixels);
    }

    [Fact]
    public void A_single_pixel_frame_survives_the_round_trip()
        => Assert.Equal([42], GifReader.Read(Encode([[42]], 1, 1)).Frames[0].Pixels);

    [Fact]
    public void No_frames_at_all_is_rejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Encode([], 8, 8));

    [Fact]
    public void A_frame_too_small_for_the_canvas_is_rejected()
        => Assert.Throws<ArgumentException>(() => Encode([Ramp(10, 0)], 8, 8));
}
