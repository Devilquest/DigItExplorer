using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Story;

/// <summary>Reconstructed story scene data containing background, palette, tall text canvas, and scroll offsets.</summary>
public sealed record StoryScene(
    byte[] Background,
    VgaPalette Palette,
    byte[] TextCanvas,
    int TextCanvasHeight,
    int[] ScrollOffsets,
    int TotalScroll);
