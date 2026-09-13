using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DigItExplorer.Core.Export;

namespace DigItExplorer.App.Services;

/// <summary>Exports bitmaps and animation sequences to disk as PNG or GIF files.</summary>
internal static class ImageExporter
{
    /// <summary>Writes a single bitmap image to disk as a PNG with optional scaling.</summary>
    internal static void WritePng(BitmapSource image, int scale, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(Magnify(image, scale)));

        using var file = File.Create(path);
        encoder.Save(file);
    }

    /// <summary>Writes an animation sequence to disk as an animated GIF with optional scaling.</summary>
    internal static void WriteGif(Models.AnimationFrames animation, int scale, string path)
    {
        var steps = new List<byte[]>(animation.Steps.Count);
        foreach (var step in animation.Steps)
        {
            steps.Add(PixelScaler.Magnify(step, animation.Width, animation.Height,
                bytesPerPixel: 1, factor: scale));
        }

        using var file = File.Create(path);
        GifEncoder.Write(file, steps, animation.Width * scale, animation.Height * scale,
            animation.Palette, animation.MsPerFrame, transparentIndex: 0, loop: animation.Loop);
    }

    /// <summary>Writes multiple frame bitmaps side by side into a horizontal strip PNG with optional scaling.</summary>
    internal static void WriteStripPng(IReadOnlyList<BitmapSource> frames, int scale, string path)
    {
        int width = frames[0].PixelWidth, height = frames[0].PixelHeight;
        int stride = width * 4;

        var buffers = new List<byte[]>(frames.Count);
        foreach (var frame in frames)
        {
            // Four bytes per pixel for every frame, which is what the one stride above assumes.
            var source = frame.Format == PixelFormats.Bgra32
                ? frame
                : new FormatConvertedBitmap(frame, PixelFormats.Bgra32, destinationPalette: null, alphaThreshold: 0);

            var pixels = new byte[stride * height];
            source.CopyPixels(pixels, stride, 0);
            buffers.Add(pixels);
        }

        var strip = BitmapSource.Create(
            width * frames.Count, height, dpiX: 96, dpiY: 96,
            PixelFormats.Bgra32, palette: null,
            FrameStrip.Compose(buffers, width, height, bytesPerPixel: 4),
            stride * frames.Count);
        strip.Freeze();

        WritePng(strip, scale, path);
    }

    /// <summary>Magnifies a bitmap image using nearest-neighbor integer scaling.</summary>
    private static BitmapSource Magnify(BitmapSource image, int scale)
    {
        if (scale == 1) return image;

        if (image.Format.BitsPerPixel < 8 || image.Format.BitsPerPixel % 8 != 0)
            image = new FormatConvertedBitmap(image, PixelFormats.Bgra32, destinationPalette: null, alphaThreshold: 0);

        int bytesPerPixel = image.Format.BitsPerPixel / 8;
        int width = image.PixelWidth, height = image.PixelHeight;
        int stride = width * bytesPerPixel;

        var pixels = new byte[stride * height];
        image.CopyPixels(pixels, stride, 0);

        var scaled = BitmapSource.Create(
            width * scale, height * scale, dpiX: 96, dpiY: 96,
            image.Format, image.Palette,
            PixelScaler.Magnify(pixels, width, height, bytesPerPixel, scale),
            stride * scale);
        scaled.Freeze();
        return scaled;
    }
}
