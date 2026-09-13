using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Reconstructs a level collision layer from tiled M.MPF material blocks.</summary>
internal static class CollisionCompositor
{
    /// <summary>Stitches and crops a level collision plane from M.MPF and DLF resources.</summary>
    /// <param name="mMpf">Raw bytes of the level collision M.MPF sheet.</param>
    /// <param name="dlf">Optional raw bytes of the level DLF record file.</param>
    /// <returns>The reconstructed CollisionImage containing raw material codes.</returns>
    public static CollisionImage Compose(byte[] mMpf, byte[]? dlf = null)
    {
        ArgumentNullException.ThrowIfNull(mMpf);

        var sheet = SheetImage.Read(mMpf);
        var frames = sheet.Frames;
        int blocks = frames.Count;

        var grid = MapGridShape.Derive(dlf, blocks);
        if (grid.Blocks == 0)
            throw new ArgumentException("M.MPF decoded to zero blocks and no .DLF states a grid.", nameof(mMpf));

        int cols = grid.Cols, rows = grid.Rows;
        int canvasW = cols * MapGridShape.BlockWidth;
        int canvasH = rows * MapGridShape.BlockHeight;

        var canvas = new byte[canvasW * canvasH];
        for (int i = 0; i < grid.Blocks - grid.MissingBlocks; i++)
        {
            var buf = frames[i];
            int cx = (i % cols) * MapGridShape.BlockWidth;
            int cy = (i / cols) * MapGridShape.BlockHeight;
            for (int y = 0; y < MapGridShape.BlockHeight; y++)
            {
                int srcOff = y * MapGridShape.BlockWidth;
                int dstOff = (cy + y) * canvasW + cx;
                buf.AsSpan(srcOff, MapGridShape.BlockWidth).CopyTo(canvas.AsSpan(dstOff, MapGridShape.BlockWidth));
            }
        }

        // The playfield is the canvas's top-left region, so a crop is a per-row prefix copy.
        int cropW = Math.Min(grid.Width, canvasW);
        int cropH = Math.Min(grid.Height, canvasH);
        byte[] codes;
        if (cropW == canvasW && cropH == canvasH)
        {
            codes = canvas;
        }
        else
        {
            codes = new byte[cropW * cropH];
            for (int y = 0; y < cropH; y++)
                canvas.AsSpan(y * canvasW, cropW).CopyTo(codes.AsSpan(y * cropW, cropW));
        }

        return new CollisionImage(cropW, cropH, cols, rows, grid.Blocks, grid.MissingBlocks, codes);
    }
}
