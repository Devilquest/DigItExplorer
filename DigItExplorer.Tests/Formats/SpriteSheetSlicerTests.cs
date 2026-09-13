using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests;

/// <summary>Verifies bounded flood-fill slicing in <see cref="SpriteSheetSlicer.Slice(byte[], byte, int, int)"/> against synthetic frames.</summary>
public class SpriteSheetSlicerTests
{
    private const byte Separator = 9;

    private static byte[] BlankFrame()
    {
        var frame = new byte[FrameCodec.Width * FrameCodec.Height];
        Array.Fill(frame, Separator);
        return frame;
    }

    // Two distinct values, not one: a uniform-color region is dropped as packing filler (see
    // SpriteSheetSlicer.Slice's own "values.Count < 2" check), so a synthetic "sprite" needs at least two.
    private static void FillRect(byte[] frame, int x, int y, int w, int h, byte value)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                frame[yy * FrameCodec.Width + xx] = value;
        frame[y * FrameCodec.Width + x] = (byte)(value + 1);
    }

    /// <summary>Guards that banded slicing confines flood-fill regions strictly within declared vertical boundaries.</summary>
    [Fact]
    public void Slice_with_a_band_does_not_flood_past_its_top_edge()
    {
        var frame = BlankFrame();
        FillRect(frame, 0, 0, FrameCodec.Width, 180, 1); // rows 0..179: one big non-separator region
        FillRect(frame, 10, 180, 3, 3, 3);               // touches row 179 directly above it
        FillRect(frame, 50, 185, 4, 2, 4);                // a second, unrelated cell further into the band

        var cells = SpriteSheetSlicer.Slice(frame, Separator, 180, 199);

        Assert.Equal(2, cells.Count);
        Assert.Equal((10, 180, 3, 3), (cells[0].X, cells[0].Y, cells[0].W, cells[0].H));
        Assert.Equal((50, 185, 4, 2), (cells[1].X, cells[1].Y, cells[1].W, cells[1].H));
    }

    [Fact]
    public void Slice_with_a_band_ignores_cells_entirely_outside_it()
    {
        var frame = BlankFrame();
        FillRect(frame, 5, 5, 4, 4, 1);      // well above the band
        FillRect(frame, 10, 180, 3, 3, 2);   // inside the band

        var cells = SpriteSheetSlicer.Slice(frame, Separator, 180, 199);

        Assert.Single(cells);
        Assert.Equal((10, 180, 3, 3), (cells[0].X, cells[0].Y, cells[0].W, cells[0].H));
    }

    /// <summary>Verifies that full-page slicing produces identical results to unconstrained slicing.</summary>
    [Fact]
    public void Slice_without_a_band_matches_slicing_the_full_page_explicitly()
    {
        var frame = BlankFrame();
        FillRect(frame, 5, 5, 4, 4, 1);
        FillRect(frame, 100, 150, 6, 6, 2);

        var unconfined = SpriteSheetSlicer.Slice(frame, Separator);
        var explicitFullRange = SpriteSheetSlicer.Slice(frame, Separator, 0, FrameCodec.Height - 1);

        Assert.Equal(unconfined.Count, explicitFullRange.Count);
        for (int i = 0; i < unconfined.Count; i++)
            Assert.Equal((unconfined[i].X, unconfined[i].Y, unconfined[i].W, unconfined[i].H),
                (explicitFullRange[i].X, explicitFullRange[i].Y, explicitFullRange[i].W, explicitFullRange[i].H));
    }
}
