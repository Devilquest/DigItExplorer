using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Minigames;

namespace DigItExplorer.Core.Menu;

/// <summary>Layer and sign visibility options for main menu canvas rendering.</summary>
public sealed record MainMenuRenderOptions(bool ShowBackground, bool ShowTerrain, bool ShowCollision,
    IReadOnlySet<(byte Category, ushort? Type)> VisibleEntityKeys,
    bool ShowMainSign, bool ShowSetup, bool ShowPlay, bool ShowIntro);

/// <summary>Rendered main menu RGB canvas, its coverage plane, and actual visibility status for each layer.</summary>
public sealed record MainMenuRenderResult(byte[] Rgb, byte[] Alpha, int Width, int Height,
    bool ShowedBackground, bool ShowedTerrain, bool ShowedCollision, bool ShowedEnemies,
    bool ShowedMainSign, bool ShowedSetup, bool ShowedPlay, bool ShowedIntro);

/// <summary>Composes the multi-layer main menu canvas including background, terrain, signs, and entities.</summary>
public static class MainMenuCompositor
{
    private static readonly HashSet<(byte Category, ushort? Type)> NoEntities = [];

    /// <summary>Composes the main menu canvas from document resources and visibility options.</summary>
    /// <param name="doc">Loaded main menu document.</param>
    /// <param name="options">Layer visibility options.</param>
    /// <param name="loadResource">Resolver function for archive resource bytes.</param>
    /// <param name="resourceExists">Predicate checking if a named resource exists.</param>
    /// <returns>Composed MainMenuRenderResult, or null if no layers are visible.</returns>
    public static MainMenuRenderResult? Compose(MainMenuDocument doc, MainMenuRenderOptions options,
        Func<string, byte[]?> loadResource, Func<string, bool> resourceExists)
    {
        bool anySign = options.ShowMainSign || options.ShowSetup || options.ShowPlay || options.ShowIntro;
        if (!options.ShowBackground && !options.ShowTerrain && !options.ShowCollision
            && options.VisibleEntityKeys.Count == 0 && !anySign)
            return null;

        var map = doc.Map;
        int w = map.Terrain.Width, h = map.Terrain.Height;

        byte[]? baseCanvas = null;
        byte[]? baseCoverage = null;
        bool showedBackground = false;
        if (options.ShowBackground)
        {
            baseCanvas = MinigameBoardCommon.ToRgb(doc.BackgroundIndices, doc.BackgroundPalette);
            baseCoverage = RgbCanvas.FullCoverage(w, h);
            showedBackground = true;
        }

        byte[] canvas, coverage;
        bool showedTerrain = false;
        if (options.ShowTerrain)
        {
            var terrainOptions = new MapRenderOptions(true, false, false, false, NoEntities, NoEntities);
            var terrainResult = MapRenderer.Render(map, terrainOptions, loadResource, resourceExists, font: null,
                baseCanvas, baseCoverage);
            canvas = terrainResult!.Rgb; // ShowTerrain: true always yields a result
            coverage = terrainResult.Alpha;
            showedTerrain = true;
        }
        else
        {
                canvas = baseCanvas is not null ? (byte[])baseCanvas.Clone() : new byte[w * h * 3];
            coverage = baseCoverage is not null ? (byte[])baseCoverage.Clone() : new byte[w * h];
        }

        bool[] wantSign = [options.ShowMainSign, options.ShowSetup, options.ShowPlay, options.ShowIntro];
        bool[] showedSign = new bool[wantSign.Length];
        for (int i = 0; i < MainMenuDocument.SignFiles.Length; i++)
        {
            if (!wantSign[i]) continue;
            string file = MainMenuDocument.SignFiles[i];
            if (!doc.Signs.TryGetValue(file, out var sheet) || sheet.Frames.Count == 0) continue;
            if (!doc.Placements.TryGetValue(file, out var placement)) continue;

            OverlaySign(canvas, coverage, w, h, sheet.Frames[0], sheet.Palette, placement);
            showedSign[i] = true;
        }
        var (showedMainSign, showedSetup, showedPlay, showedIntro) = (showedSign[0], showedSign[1], showedSign[2], showedSign[3]);

        bool showedCollision = false, showedEnemies = false;
        if (options.ShowCollision || options.VisibleEntityKeys.Count > 0)
        {

            var entityOptions = new MapRenderOptions(false, options.ShowCollision, false, false,
                options.VisibleEntityKeys, options.VisibleEntityKeys);
            var entityResult = MapRenderer.Render(map, entityOptions, loadResource, resourceExists, font: null,
                canvas, coverage);
            if (entityResult is not null)
            {
                canvas = entityResult.Rgb;
                coverage = entityResult.Alpha;
                showedCollision = entityResult.ShowedCollision;
                showedEnemies = entityResult.ShowedEntities;
            }
        }

        return new MainMenuRenderResult(canvas, coverage, w, h, showedBackground, showedTerrain, showedCollision, showedEnemies,
            showedMainSign, showedSetup, showedPlay, showedIntro);
    }

    /// <summary>Overlays a sign page frame onto the canvas using its derived color key transparency.</summary>
    private static void OverlaySign(byte[] rgb, byte[] coverage, int canvasW, int canvasH, byte[] page, VgaPalette palette,
        MainMenuSignAnchors.Placement placement)
    {
        var pal = palette.Rgb;
        int rows = Math.Min(FrameCodec.Height, canvasH);
        for (int y = 0; y < rows; y++)
        {
            int pRow = y * FrameCodec.Width;
            int outRow = y * canvasW;
            for (int x = 0; x < FrameCodec.Width; x++)
            {
                int cx = placement.Anchor + x;
                if (cx >= canvasW) continue;
                byte v = page[pRow + x];
                if (v == placement.ColorKey) continue;
                RgbCanvas.PaintIndex(rgb, coverage, outRow + cx, pal, v);
            }
        }
    }
}
