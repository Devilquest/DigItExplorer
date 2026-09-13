namespace DigItExplorer.Core.Maps;

/// <summary>Decomposed raster layer of a world map with palette indices and optional alpha mask.</summary>
public sealed class WorldMapLayer
{
    /// <summary>Canvas width in pixels.</summary>
    public int Width { get; }

    /// <summary>Canvas height in pixels (200).</summary>
    public int Height { get; }

    /// <summary>Row-major palette indices.</summary>
    public byte[] Indices { get; }

    /// <summary>Per-pixel alpha coverage buffer, or null if fully opaque.</summary>
    public byte[]? Alpha { get; }

    internal WorldMapLayer(int width, int height, byte[] indices, byte[]? alpha)
    {
        Width = width;
        Height = height;
        Indices = indices;
        Alpha = alpha;
    }
}
