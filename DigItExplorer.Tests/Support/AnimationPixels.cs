using System.Security.Cryptography;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests;

/// <summary>Renders sprite cells to RGBA buffers for the animation guard tests' frame hashes.</summary>
internal static class AnimationPixels
{
    /// <summary>Renders one cell to RGBA: transparent indices become fully transparent, everything
    /// else the palette color at full alpha.</summary>
    /// <param name="cell">The sprite cell to render.</param>
    /// <param name="palette">Palette resolving each pixel's color.</param>
    /// <param name="rgba">Destination buffer, cleared before the cell is drawn.</param>
    /// <param name="alsoTransparent">Extra palette indices to treat as transparent alongside index 0.</param>
    public static void ToRgba(SpriteCell cell, VgaPalette palette, byte[] rgba,
        IReadOnlySet<byte>? alsoTransparent = null)
    {
        Array.Clear(rgba);
        for (int p = 0; p < cell.Pixels.Length; p++)
        {
            byte v = cell.Pixels[p];
            if (v == 0 || alsoTransparent?.Contains(v) == true) continue;
            var (r, g, b) = palette[v];
            int o = p * 4;
            rgba[o] = r;
            rgba[o + 1] = g;
            rgba[o + 2] = b;
            rgba[o + 3] = 255;
        }
    }

    /// <summary>Hashes one cell's rendered RGBA, for the sheets pinned cell by cell rather than by animation.</summary>
    /// <param name="cell">The sprite cell to render and hash.</param>
    /// <param name="palette">Palette resolving each pixel's color.</param>
    /// <returns>The uppercase hex SHA-256 of the cell's RGBA bytes.</returns>
    public static string Sha256Rgba(SpriteCell cell, VgaPalette palette)
    {
        var rgba = new byte[cell.W * cell.H * 4];
        ToRgba(cell, palette, rgba);
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(rgba));
    }
}
