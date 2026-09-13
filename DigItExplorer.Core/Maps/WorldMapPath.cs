namespace DigItExplorer.Core.Maps;

/// <summary>World map path layout containing walk polyline pixels, stop pixels, and node anchors.</summary>
public sealed record WorldMapPath(
    IReadOnlyList<(int X, int Y)> PathPixels,
    IReadOnlyList<(int X, int Y)> StopPixels,
    IReadOnlyDictionary<int, (int X, int Y)> Nodes);
