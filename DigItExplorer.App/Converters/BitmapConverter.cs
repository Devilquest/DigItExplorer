using System.Windows.Media;
using System.Windows.Media.Imaging;
using DigItExplorer.Core.Export;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.App.Converters;

/// <summary>Converts Core pixel buffers to frozen WPF <see cref="BitmapSource"/> instances.</summary>
internal static class BitmapConverter
{
    /// <summary>Converts a sprite cell to a 32-bit BGRA bitmap with optional stage padding and transparency keys.</summary>
    /// <param name="cell">The sprite cell to render.</param>
    /// <param name="palette">The VGA palette providing RGB colors.</param>
    /// <param name="alsoTransparent">Additional palette indices to treat as transparent.</param>
    /// <param name="stageW">Target stage width to pad and center the cell within, or 0 for no padding.</param>
    /// <param name="stageH">Target stage height to pad and center the cell within, or 0 for no padding.</param>
    public static BitmapSource CellToBitmap(SpriteCell cell, VgaPalette palette,
        IReadOnlySet<byte>? alsoTransparent = null, int stageW = 0, int stageH = 0)
    {
        // Canvas placement arithmetic is centralized in Core so export and display layouts match.
        var placed = FrameCanvas.Place(cell, stageW, stageH, alsoTransparent);
        return PlacedToBitmap(placed, palette);
    }

    /// <summary>Converts a placed indexed frame to a 32-bit BGRA bitmap with transparent key 0.</summary>
    public static BitmapSource PlacedToBitmap(PlacedFrame placed, VgaPalette palette)
    {
        int w = placed.Width, h = placed.Height;
        var bgra = new byte[w * h * 4];

        for (int p = 0; p < placed.Indices.Length; p++)
        {
            byte v = placed.Indices[p];
            if (v == 0) continue;
            var (r, g, b) = palette[v];
            int o = p * 4;
            bgra[o] = b;
            bgra[o + 1] = g;
            bgra[o + 2] = r;
            bgra[o + 3] = 255;
        }

        var bitmap = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, bgra, w * 4);
        bitmap.Freeze();
        return bitmap;
    }

    public static BitmapSource ToBitmap(SheetImage sheet, int frameIndex)
        => ToIndexed8(FrameCodec.Width, FrameCodec.Height, sheet.Frames[frameIndex], sheet.Palette);

    public static BitmapSource ToBitmapRgb24(byte[] rgb, int width, int height)
    {
        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb24, null, rgb, width * 3);
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>Combines RGB pixel bytes and an alpha coverage plane into a 32-bit BGRA bitmap.</summary>
    public static BitmapSource ToBitmapRgba(byte[] rgb, byte[] alpha, int width, int height)
    {
        var bgra = new byte[width * height * 4];
        for (int p = 0; p < alpha.Length; p++)
        {
            if (alpha[p] == 0) continue;
            int i = p * 3, o = p * 4;
            bgra[o] = rgb[i + 2];
            bgra[o + 1] = rgb[i + 1];
            bgra[o + 2] = rgb[i];
            bgra[o + 3] = alpha[p];
        }

        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bgra, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    public static BitmapSource ToIndexed8(int width, int height, byte[] indices, VgaPalette palette)
        => ToIndexed8(width, height, indices, ToBitmapPalette(palette));

    /// <summary>Creates an 8-bit indexed bitmap using a pre-constructed <see cref="BitmapPalette"/>.</summary>
    public static BitmapSource ToIndexed8(int width, int height, byte[] indices, BitmapPalette palette)
    {
        var bitmap = BitmapSource.Create(
            width, height, dpiX: 96, dpiY: 96,
            PixelFormats.Indexed8, palette,
            indices, stride: width);
        bitmap.Freeze();
        return bitmap;
    }

    public static BitmapPalette ToBitmapPalette(VgaPalette palette)
    {
        var rgb = palette.Rgb;
        var colors = new List<Color>(256);
        for (int i = 0; i < 256; i++)
            colors.Add(Color.FromRgb(rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2]));
        return new BitmapPalette(colors);
    }
}
