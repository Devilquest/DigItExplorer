using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Reconstructs world map raster layers, paths, and textures from game files.</summary>
internal static class WorldMapCompositor
{
    private const int BlockWidth = MapGridShape.BlockWidth;   // 320
    private const int BlockHeight = MapGridShape.BlockHeight; // 200

    /// <summary>Composes the flattened MAPxxLD plane as a standalone world map image.</summary>
    /// <param name="ldMpf">Raw bytes of the LD.MPF base sheet.</param>
    /// <param name="pal">Optional 768-byte VGA palette.</param>
    /// <returns>The decoded WorldMapImage.</returns>
    public static WorldMapImage ComposeBase(byte[] ldMpf, byte[]? pal = null)
    {
        ArgumentNullException.ThrowIfNull(ldMpf);

        var (width, height, blocks, indices) = WorldMapPlane.Decode(ldMpf);
        var palette = pal is { Length: >= 768 }
            ? VgaPalette.From6Bit(pal.AsSpan(0, 768))
            : SheetImage.Read(ldMpf).Palette;

        return new WorldMapImage(width, height, blocks, palette, indices);
    }

    /// <summary>Composes the Front terrain silhouette layer with index 0 transparency.</summary>
    public static WorldMapLayer ComposeFront(byte[] fMpf)
    {
        ArgumentNullException.ThrowIfNull(fMpf);

        var (width, height, _, indices) = WorldMapPlane.Decode(fMpf);
        var alpha = new byte[indices.Length];
        for (int i = 0; i < indices.Length; i++)
            alpha[i] = indices[i] == 0 ? (byte)0 : (byte)255;

        return new WorldMapLayer(width, height, indices, alpha);
    }

    /// <summary>Wraps an existing WorldMapImage as a fully opaque Front layer fallback.</summary>
    public static WorldMapLayer FrontFromBase(WorldMapImage baseImage)
        => new(baseImage.Width, baseImage.Height, baseImage.Indices, alpha: null);

    /// <summary>Composes the Sky background layer by tiling texture horizontally across the canvas.</summary>
    public static WorldMapLayer ComposeSky(byte[] skySpf, int canvasWidth)
    {
        ArgumentNullException.ThrowIfNull(skySpf);

        var frame = SheetImage.Read(skySpf).Frames[0];
        var indices = TileHorizontally(frame, canvasWidth);
        return new WorldMapLayer(canvasWidth, BlockHeight, indices, alpha: null);
    }

    /// <summary>Composes the Background layer, optionally applying a binary transparency mask.</summary>
    public static WorldMapLayer ComposeBackground(byte[] bk, byte[]? mask, int canvasWidth)
    {
        ArgumentNullException.ThrowIfNull(bk);

        var frame = SheetImage.Read(bk).Frames[0];
        var indices = TileHorizontally(frame, canvasWidth);

        byte[]? alpha = null;
        if (mask is not null)
        {
            var maskFrame = SheetImage.Read(mask).Frames[0];
            var tiledMask = TileHorizontally(maskFrame, canvasWidth);
            alpha = new byte[tiledMask.Length];
            for (int i = 0; i < tiledMask.Length; i++)
                alpha[i] = tiledMask[i] == 0 ? (byte)255 : (byte)0;
        }

        return new WorldMapLayer(canvasWidth, BlockHeight, indices, alpha);
    }

    /// <summary>Tiles a 320x200 frame horizontally to fill a target canvas width.</summary>
    private static byte[] TileHorizontally(byte[] frame320x200, int canvasWidth)
    {
        var canvas = new byte[canvasWidth * BlockHeight];
        for (int y = 0; y < BlockHeight; y++)
        {
            for (int cx = 0; cx < canvasWidth; cx += BlockWidth)
            {
                int copyW = Math.Min(BlockWidth, canvasWidth - cx);
                Array.Copy(frame320x200, y * BlockWidth, canvas, y * canvasWidth + cx, copyW);
            }
        }
        return canvas;
    }

    /// <summary>Decodes walk paths, stop pixels, and node anchors from the path plane bytes.</summary>
    public static WorldMapPath ComposePath(byte[] pMpf)
    {
        ArgumentNullException.ThrowIfNull(pMpf);

        var (width, _, _, codes) = WorldMapPlane.Decode(pMpf);
        var path = new List<(int X, int Y)>();
        var stops = new List<(int X, int Y)>();
        var nodes = new Dictionary<int, (int X, int Y)>();

        for (int i = 0; i < codes.Length; i++)
        {
            byte v = codes[i];
            if (v == 0) continue;
            int x = i % width, y = i / width;
            if (v == 254) path.Add((x, y));
            else if (v == 253) stops.Add((x, y));
            else nodes[v - 16] = (x, y);
        }

        return new WorldMapPath(path, stops, nodes);
    }
}
