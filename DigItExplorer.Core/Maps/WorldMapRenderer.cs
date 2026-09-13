using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Core.Maps;

/// <summary>Layer and sign visibility options for world map rendering, with one flag per sign kind because
/// a map's nodes carry several kinds at once and each is worth seeing on its own.</summary>
public sealed record WorldMapRenderOptions(bool ShowSky, bool ShowBackground, bool ShowFront, bool ShowPath,
    bool ShowLevelSigns, bool ShowCheckpointSigns, bool ShowGateSigns, bool ShowTraceSigns);

/// <summary>Rendered world map RGB canvas, its coverage plane, and actual visibility status for each layer.</summary>
public sealed record WorldMapRenderResult(byte[] Rgb, byte[] Alpha, int Width, int Height,
    bool ShowedSky, bool ShowedBackground, bool ShowedFront, bool ShowedPath,
    bool ShowedLevelSigns, bool ShowedCheckpointSigns, bool ShowedGateSigns, bool ShowedTraceSigns);

/// <summary>Composes multi-layer world map preview canvases from documents and visibility options.</summary>
public static class WorldMapRenderer
{
    private static readonly (byte R, byte G, byte B) PathColor = (255, 255, 0);
    private static readonly (byte R, byte G, byte B) StopColor = (255, 128, 0);

    /// <summary>Composes the layer stack and sign overlays for a world map.</summary>
    /// <param name="map">Decoded world map document.</param>
    /// <param name="options">Layer visibility options.</param>
    /// <returns>The composed WorldMapRenderResult, or null if no layers are visible.</returns>
    public static WorldMapRenderResult? Render(WorldMapDocument map, WorldMapRenderOptions options)
    {
        bool showSky = options.ShowSky && map.Sky is not null;
        bool showBackground = options.ShowBackground && map.Background is not null;
        bool showFront = options.ShowFront;
        bool showPath = options.ShowPath && map.Path is not null;

        bool canShowSigns = map.Path is not null && map.SignSheet is not null;
        bool showLevelSigns = options.ShowLevelSigns && canShowSigns;
        bool showCheckpointSigns = options.ShowCheckpointSigns && canShowSigns;
        bool showGateSigns = options.ShowGateSigns && canShowSigns;
        bool showTraceSigns = options.ShowTraceSigns && canShowSigns;
        bool showAnySigns = showLevelSigns || showCheckpointSigns || showGateSigns || showTraceSigns;

        if (!showSky && !showBackground && !showFront && !showPath && !showAnySigns) return null;

        int w = map.Width, h = map.Height;
        byte[] rgb = new byte[w * h * 3];
        byte[] alpha = new byte[w * h];

        if (showSky) PaintLayer(rgb, alpha, w, h, map.Sky!, map.Palette);
        if (showBackground) PaintLayer(rgb, alpha, w, h, map.Background!, map.Palette);
        if (showFront) PaintLayer(rgb, alpha, w, h, map.Front, map.Palette);

        if (showPath)
        {
            DrawPoints(rgb, alpha, w, h, map.Path!.PathPixels, PathColor);
            DrawPoints(rgb, alpha, w, h, map.Path!.StopPixels, StopColor);
        }

        if (showAnySigns)
        {
            var visibleSignTypes = new HashSet<SignType>();
            if (showLevelSigns) visibleSignTypes.Add(SignType.Level);
            if (showCheckpointSigns) visibleSignTypes.Add(SignType.Checkpoint);
            if (showGateSigns) visibleSignTypes.Add(SignType.Gate);
            if (showTraceSigns) visibleSignTypes.Add(SignType.Trace);

            WorldMapSignStamper.Stamp(rgb, alpha, w, h, map.Path!, map.World, map.SignOf,
                map.SignSheet!, map.Palette, visibleSignTypes);
        }

        return new WorldMapRenderResult(rgb, alpha, w, h, showSky, showBackground, showFront, showPath,
            showLevelSigns, showCheckpointSigns, showGateSigns, showTraceSigns);
    }

    /// <summary>Paints an indexed raster layer onto the RGB canvas with alpha masking.</summary>
    private static void PaintLayer(byte[] rgb, byte[] coverage, int width, int height, WorldMapLayer layer,
        VgaPalette palette)
    {
        var pal = palette.Rgb;
        var mask = layer.Alpha;
        for (int i = 0; i < layer.Indices.Length; i++)
        {
            if (mask is not null && mask[i] == 0) continue;
            RgbCanvas.PaintIndex(rgb, coverage, i, pal, layer.Indices[i]);
        }
    }

    private static void DrawPoints(byte[] rgb, byte[] coverage, int width, int height,
        IReadOnlyList<(int X, int Y)> points, (byte R, byte G, byte B) color)
    {
        foreach (var (x, y) in points)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) continue;
            RgbCanvas.Paint(rgb, coverage, y * width + x, color.R, color.G, color.B);
        }
    }
}
