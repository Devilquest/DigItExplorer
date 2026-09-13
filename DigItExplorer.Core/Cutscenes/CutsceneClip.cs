using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Cutscenes;

/// <summary>Reconstructed cutscene clip with decoded frames, playback order, timing, and caption overlay.</summary>
public sealed record CutsceneClip(
    IReadOnlyList<byte[]> Frames,
    VgaPalette Palette,
    IReadOnlyList<int> FrameOrder,
    double FrameMs,
    CaptionOverlay? Caption);

/// <summary>Pre-rendered caption text glyph pixels and horizontal screen anchor.</summary>
public sealed record CaptionOverlay(byte[] Pixels, int Width, int Height, int X);
