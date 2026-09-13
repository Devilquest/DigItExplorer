using DigItExplorer.Core.Formats;

namespace DigItExplorer.App.Models;

/// <summary>Animation playback metadata and indexed frame step sequence for export.</summary>
/// <param name="Steps">Ordered sequence of animation frame pixel buffers.</param>
/// <param name="Width">Frame pixel width.</param>
/// <param name="Height">Frame pixel height.</param>
/// <param name="Palette">VGA palette used for color indexing.</param>
/// <param name="MsPerFrame">Frame duration interval in milliseconds.</param>
/// <param name="Loop">Whether animation loops indefinitely.</param>
internal sealed record AnimationFrames(
    IReadOnlyList<byte[]> Steps,
    int Width,
    int Height,
    VgaPalette Palette,
    double MsPerFrame,
    bool Loop);
