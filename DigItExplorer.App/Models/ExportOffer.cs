using System.Windows.Media.Imaging;

namespace DigItExplorer.App.Models;

/// <summary>Export capabilities and source data offered by the active preview document.</summary>
/// <param name="Shape">Structural classification of exportable content.</param>
/// <param name="Subject">Base subject name for exported files.</param>
/// <param name="FrameCount">Total number of available frames.</param>
/// <param name="Frame">Delegate resolving a specific frame index to a bitmap.</param>
/// <param name="Animation">Animation metadata if animated export is available.</param>
internal sealed record ExportOffer(
    ExportShape Shape,
    string Subject,
    int FrameCount = 1,
    Func<int, BitmapSource>? Frame = null,
    AnimationFrames? Animation = null);

/// <summary>Target output format selected for export.</summary>
internal enum ExportFormat
{
    /// <summary>Exports the active single frame.</summary>
    CurrentFrame,

    /// <summary>Exports all frames as individual image files.</summary>
    FrameSequence,

    /// <summary>Exports all frames stitched into a single horizontal strip.</summary>
    FrameStrip,

    /// <summary>Exports animation sequence as an animated GIF.</summary>
    AnimatedGif,
}

/// <summary>User selections returned from the export dialog.</summary>
/// <param name="Format">Selected export format.</param>
/// <param name="Scale">Magnification scale factor.</param>
/// <param name="FormatWasChosen">Whether a specific format choice was explicitly made.</param>
internal sealed record ExportChoice(ExportFormat Format, int Scale, bool FormatWasChosen);
