using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Reconstructs a level terrain layer from tiled MPF blocks and DLF geometry.</summary>
internal static class TerrainCompositor
{
    private const int BlockWidth = MapGridShape.BlockWidth;   // 320
    private const int BlockHeight = MapGridShape.BlockHeight;  // 200

    /// <summary>Stitches and crops a level terrain playfield from MPF, DLF, and PAL resources.</summary>
    /// <param name="fMpf">Raw bytes of the level foreground MPF sheet.</param>
    /// <param name="dlf">Optional raw bytes of the level DLF record file.</param>
    /// <param name="pal">Optional 768-byte VGA palette.</param>
    /// <returns>The reconstructed TerrainImage.</returns>
    public static TerrainImage Compose(byte[] fMpf, byte[]? dlf = null, byte[]? pal = null)
    {
        ArgumentNullException.ThrowIfNull(fMpf);

        var sheet = SheetImage.Read(fMpf);
        var frames = sheet.Frames;
        int blocks = frames.Count;

        var palette = pal is { Length: >= 768 } ? VgaPalette.From6Bit(pal.AsSpan(0, 768)) : sheet.Palette;

        var grid = MapGridShape.Derive(dlf, blocks);
        if (grid.Blocks == 0)
            throw new ArgumentException("F.MPF decoded to zero blocks and no .DLF states a grid.", nameof(fMpf));

        int cols = grid.Cols, rows = grid.Rows;
        int canvasW = cols * BlockWidth;
        int canvasH = rows * BlockHeight;

        var canvas = new byte[canvasW * canvasH];
        for (int i = 0; i < grid.Blocks - grid.MissingBlocks; i++)
        {
            var buf = frames[i];
            int cx = (i % cols) * BlockWidth;
            int cy = (i / cols) * BlockHeight;
            for (int y = 0; y < BlockHeight; y++)
            {
                int srcOff = y * BlockWidth;
                int dstOff = (cy + y) * canvasW + cx;
                buf.AsSpan(srcOff, BlockWidth).CopyTo(canvas.AsSpan(dstOff, BlockWidth));
            }
        }

        // The playfield is the canvas's top-left region, so a crop is a per-row prefix copy.
        int cropW = Math.Min(grid.Width, canvasW);
        int cropH = Math.Min(grid.Height, canvasH);
        byte[] indices;
        if (cropW == canvasW && cropH == canvasH)
        {
            indices = canvas;
        }
        else
        {
            indices = new byte[cropW * cropH];
            for (int y = 0; y < cropH; y++)
                canvas.AsSpan(y * canvasW, cropW).CopyTo(indices.AsSpan(y * cropW, cropW));
        }

        return new TerrainImage(cropW, cropH, cols, rows, grid.Blocks, grid.MissingBlocks, palette, indices);
    }
}
