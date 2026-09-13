using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Export;

/// <summary>Single animation frame placed on a canvas with palette indices and dimensions.</summary>
public readonly record struct PlacedFrame(byte[] Indices, int Width, int Height);

/// <summary>Places and centers sprite cells onto animation stage canvases.</summary>
public static class FrameCanvas
{
    /// <summary>Centers a sprite cell on a canvas of at least stageWidth by stageHeight.</summary>
    /// <param name="cell">Sprite cell to place.</param>
    /// <param name="stageWidth">Minimum canvas width.</param>
    /// <param name="stageHeight">Minimum canvas height.</param>
    /// <param name="alsoTransparent">Optional set of additional transparent palette indices.</param>
    /// <returns>A PlacedFrame containing centered canvas indices.</returns>
    public static PlacedFrame Place(SpriteCell cell, int stageWidth, int stageHeight,
        IReadOnlySet<byte>? alsoTransparent = null)
    {
        int width = Math.Max(stageWidth, cell.W);
        int height = Math.Max(stageHeight, cell.H);
        int padX = (width - cell.W) / 2;
        int padY = (height - cell.H) / 2;

        var indices = new byte[width * height];
        for (int p = 0; p < cell.Pixels.Length; p++)
        {
            byte value = cell.Pixels[p];
            if (value == 0 || alsoTransparent?.Contains(value) == true) continue;
            indices[(p / cell.W + padY) * width + p % cell.W + padX] = value;
        }

        return new PlacedFrame(indices, width, height);
    }
}
