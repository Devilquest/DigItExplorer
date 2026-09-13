using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Core.Maps;

/// <summary>Layer visibility options for map rendering.</summary>
public sealed record MapRenderOptions(bool ShowTerrain, bool ShowCollision, bool ShowExitInfo, bool ShowBonusInfo,
    IReadOnlySet<(byte Category, ushort? Type)> VisibleEntityKeys,
    IReadOnlySet<(byte Category, ushort? Type)> VisibleColliderKeys);

/// <summary>Rendered RGB canvas, its coverage plane, and actual visibility status for map layers.</summary>
public sealed record MapRenderResult(byte[] Rgb, byte[] Alpha, int Width, int Height,
    bool ShowedTerrain, bool ShowedCollision, bool ShowedEntities);

/// <summary>Composes multi-layer map preview canvases from map documents and visibility settings.</summary>
public static class MapRenderer
{
    /// <summary>Whole-number size multiplier for destination-info labels.</summary>
    private const int InfoTextScale = 2;

    /// <summary>Font palette ramp style index for exits back to the world map (red ramp).</summary>
    private const int MapExitStyle = 2;

    /// <summary>Font palette ramp style index for exits into another level (amber ramp).</summary>
    private const int LevelExitStyle = 5;

    /// <summary>Font palette ramp style index for exits that lead back into the same level (gray ramp).</summary>
    private const int DeadEndStyle = 1;

    /// <summary>Font palette ramp style index for exits into a bonus zone (green ramp).</summary>
    private const int BonusInfoStyle = 3;

    /// <summary>Material code for open void collision space.</summary>
    private const byte OpenCode = 15;

    /// <summary>Composes the multi-layer RGB preview canvas for a level map.</summary>
    /// <param name="map">Decoded level map document.</param>
    /// <param name="options">Layer visibility options.</param>
    /// <param name="loadResource">Resolver function for archive resource bytes.</param>
    /// <param name="resourceExists">Predicate checking resource existence.</param>
    /// <param name="font">Optional game font for drawing text labels.</param>
    /// <param name="baseCanvas">Optional pre-filled base canvas for backdrop layering.</param>
    /// <param name="baseCoverage">Coverage plane of <paramref name="baseCanvas"/>, or null for a fully painted base.</param>
    /// <returns>Composed MapRenderResult, or null if no layers are visible.</returns>
    public static MapRenderResult? Render(MapDocument map, MapRenderOptions options,
        Func<string, byte[]?> loadResource, Func<string, bool> resourceExists, GameFont? font = null,
        byte[]? baseCanvas = null, byte[]? baseCoverage = null)
    {
        bool showTerrain = options.ShowTerrain;
        bool showCollisionPlane = options.ShowCollision && map.Collision is not null;
        // Ticked footprints are worth a collision pass of their own: with the level's plane off they draw
        // over an otherwise open plane, which is what "the platforms' collision and nothing else" looks like.
        var visibleStamps = map.Collision is not null
            ? map.ColliderStamps.Where(s => options.VisibleColliderKeys.Contains(s.LayerKey)).ToList()
            : [];
        bool showCollision = showCollisionPlane || visibleStamps.Count > 0;
        var visibleEntities = map.Entities.Where(e => options.VisibleEntityKeys.Contains(EntityCategories.KeyOf(e))).ToList();
        bool showEntities = visibleEntities.Count > 0;

        // Destination-info overlays toggle independently of the Dig Spot/Exit Sign/Drain checkboxes, so they
        // read straight from map.Entities rather than the visibility-filtered visibleEntities.
        var infoOverlayEntities = (options.ShowExitInfo || options.ShowBonusInfo)
            ? map.Entities.Where(e => e.Category == 0x05 || e.Category == 0x5B || e.Category == 0x5A).ToList()
            : new List<DlfRecord>();
        bool showInfoOverlay = infoOverlayEntities.Count > 0;

        if (!showTerrain && !showCollision && !showEntities && !showInfoOverlay) return null;

        // Base canvas: terrain or a blank canvas; sized off Terrain, which is always loaded once a map
        // document exists (see MapDocument.Load).
        var terrain = map.Terrain;
        int w = terrain.Width, h = terrain.Height;
        if (showCollision)
        {
            var collision = map.Collision!;
            w = Math.Min(w, collision.Width);
            h = Math.Min(h, collision.Height);
        }

        byte[] rgb = baseCanvas is not null ? (byte[])baseCanvas.Clone() : new byte[w * h * 3];
        byte[] alpha = BaseCoverage(baseCanvas, baseCoverage, w, h);

        if (showTerrain)
        {
            var pal = terrain.Palette.Rgb;
            var tIdx = terrain.Indices;
            int tStride = terrain.Width;
            for (int y = 0; y < h; y++)
            {
                int tRow = y * tStride, outRow = y * w;
                for (int x = 0; x < w; x++)
                {
                    int idx = tIdx[tRow + x];
                    if (baseCanvas is not null && idx == 0) continue; // let the pre-filled canvas show through
                    RgbCanvas.PaintIndex(rgb, alpha, outRow + x, pal, (byte)idx);
                }
            }
        }

        if (showEntities)
        {
            var remaining = visibleEntities;
            if (map.RealPalette is { } realPalette)
            {
                // Enemies/Aquatics/Ghosts stamp first so plants (last within Stamp's own draw order) still
                // end up on top of them, matching the game's foreground-decoration depth rule (see
                // EntityArtStamper.Stamp). WO_FISH/WO_GHOST aren't world-suffixed, but ResolveWorldSheet
                // falls through to the bare name when no suffixed variant exists, so it works for both.
                Func<string, byte[]?> loadWorldSheet = baseName => WorldSheets.ResolveWorldSheet(baseName, map.Stem, loadResource);
                var stamped = EntityArtStamper.StampEnemies(rgb, alpha, w, h, visibleEntities, realPalette, loadWorldSheet);
                stamped.UnionWith(EntityArtStamper.StampSpeciesGrids(rgb, alpha, w, h, visibleEntities, realPalette, loadWorldSheet));
                stamped.UnionWith(EntityArtStamper.StampSpawns(rgb, alpha, w, h, visibleEntities, realPalette,
                    loadDugSpawn: () => WorldSheets.ResolveDugSpawnSheet(map.Stem, loadResource),
                    loadWoBoss: () => loadResource("WO_BOSS.MPF")));
                stamped.UnionWith(EntityArtStamper.StampMechanisms(rgb, alpha, w, h, visibleEntities, realPalette,
                    loadWorldSheet, loadResource));
                stamped.UnionWith(EntityArtStamper.StampMarkers(rgb, alpha, w, h, visibleEntities, realPalette,
                    map.Stem, loadResource));
                stamped.UnionWith(EntityArtStamper.Stamp(rgb, alpha, w, h, visibleEntities, realPalette, loadResource));
                if (stamped.Count > 0)
                    remaining = visibleEntities.Where((_, i) => !stamped.Contains(i)).ToList();
            }
            StampEntityMarkers(rgb, alpha, w, h, remaining);
        }

        if (showInfoOverlay && font is not null && map.RealPalette is { } labelPalette)
        {
            var mask = new byte[w * h];
            var mapExitRamp = GameFont.Ramp(MapExitStyle);
            var levelExitRamp = GameFont.BrightRamp(LevelExitStyle);
            var deadEndRamp = GameFont.Ramp(DeadEndStyle);
            var bonusRamp = GameFont.BrightRamp(BonusInfoStyle);
            bool drewLabel = false;

            foreach (var rec in infoOverlayEntities)
            {
                int slot = ExitDestinations.DestinationSlotOf(rec, map.Stem, resourceExists) ?? -1;
                bool toBonus = ExitDestinations.LeadsToBonusZone(map.Stem, slot, loadResource);
                bool showInfo = toBonus ? options.ShowBonusInfo : options.ShowExitInfo;
                if (!showInfo) continue;

                string labelStr = ">" + ExitDestinations.GetDestLabel(map.Stem, slot);
                int targetW = rec.Category == 0x05 ? 32 : (rec.Category == 0x5B ? 89 : 28);
                int targetH = rec.Category == 0x05 ? 32 : (rec.Category == 0x5B ? 50 : 26);

                var labelBytes = System.Text.Encoding.ASCII.GetBytes(labelStr);
                // Account for the extra rendered pixel column beyond measured font width.
                int labelWidth = (font.TextWidth(labelBytes) + 1) * InfoTextScale;
                int labelHeight = GameFontLabel.Height(font, labelBytes) * InfoTextScale;

                int drawX = rec.X + (targetW - labelWidth) / 2;
                int drawY = rec.Y + (targetH - labelHeight) / 2;
                if (rec.Category == 0x5A)
                {
                    drawY -= 20;
                }

                drawX = ClampToCanvas(drawX, labelWidth, w);
                drawY = ClampToCanvas(drawY, labelHeight, h);

                var ramp = toBonus ? bonusRamp
                    : ExitDestinations.LeadsBackToItself(map.Stem, slot) ? deadEndRamp
                    : ExitDestinations.IsWorldMap(slot) ? mapExitRamp
                    : levelExitRamp;
                GameFontLabel.DrawScaled(font, mask, w, h, drawX, drawY, labelBytes, ramp, InfoTextScale);
                drewLabel = true;
            }

            if (drewLabel) GameFontLabel.Composite(rgb, alpha, mask, w, h, labelPalette);
        }

        if (showCollision)
        {
            OverlayCollisionRgb(rgb, alpha, w, h, map.Collision!, visibleStamps, showCollisionPlane);
        }

        return new MapRenderResult(rgb, alpha, w, h, showTerrain, showCollision, showEntities);
    }

    /// <summary>Builds the coverage plane a render starts from, matching the base canvas it layers over.</summary>
    private static byte[] BaseCoverage(byte[]? baseCanvas, byte[]? baseCoverage, int width, int height)
    {
        if (baseCanvas is null) return new byte[width * height];
        if (baseCoverage is null) return RgbCanvas.FullCoverage(width, height);

        // The plane always matches the render's own dimensions, which a collision layer can shrink.
        var coverage = new byte[width * height];
        Array.Copy(baseCoverage, coverage, Math.Min(baseCoverage.Length, coverage.Length));
        return coverage;
    }

    /// <summary>Clamps label coordinate bounds within the canvas leaving outline margin.</summary>
    private static int ClampToCanvas(int position, int extent, int canvas)
    {
        const int outlineMargin = 1;
        return Math.Clamp(position, outlineMargin, Math.Max(outlineMargin, canvas - extent - outlineMargin));
    }

    /// <summary>Blends false-color collision layer and stamped footprints over the canvas in-place.</summary>
    private static void OverlayCollisionRgb(byte[] rgb, byte[] alpha, int w, int h, CollisionImage collision,
        IReadOnlyList<ColliderStamp> stamps, bool includeLevelPlane)
    {
        byte[] codes;
        if (includeLevelPlane)
        {
            codes = collision.MaterialCodes;
            if (stamps.Count > 0)
            {
                codes = (byte[])codes.Clone();
                ColliderStamper.Apply(codes, collision.Width, collision.Height, stamps);
            }
        }
        else
        {
            codes = new byte[collision.MaterialCodes.Length];
            Array.Fill(codes, OpenCode);
            ColliderStamper.Apply(codes, collision.Width, collision.Height, stamps);
        }
        int cStride = collision.Width;

        for (int y = 0; y < h; y++)
        {
            int cRow = y * cStride, outRow = y * w;
            for (int x = 0; x < w; x++)
            {
                var (cr, cg, cb, ca) = MaterialPalette.OverlayColorOf(codes[cRow + x]);
                RgbCanvas.Blend(rgb, alpha, outRow + x, cr, cg, cb, ca);
            }
        }
    }

    /// <summary>Draws fallback color-coded square markers for unstamped entity records.</summary>
    private static void StampEntityMarkers(byte[] rgb, byte[] alpha, int width, int height,
        IReadOnlyList<DlfRecord> records)
    {
        const int markerSize = 8;
        foreach (var rec in records)
        {
            var (r, g, b) = EntityCategories.ColorOf(rec.Category, rec.Type);
            for (int dy = 0; dy < markerSize; dy++)
            {
                int py = rec.Y + dy;
                if (py < 0 || py >= height) continue;
                int rowOff = py * width;
                for (int dx = 0; dx < markerSize; dx++)
                {
                    int px = rec.X + dx;
                    if (px < 0 || px >= width) continue;
                    RgbCanvas.Paint(rgb, alpha, rowOff + px, r, g, b);
                }
            }
            if (rec.X >= 0 && rec.X < width && rec.Y >= 0 && rec.Y < height)
            {
                RgbCanvas.Paint(rgb, alpha, rec.Y * width + rec.X, 255, 255, 255);
            }
        }
    }
}
