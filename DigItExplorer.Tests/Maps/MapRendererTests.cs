using System.Security.Cryptography;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="MapRenderer"/>'s layer toggling and pixel-level compositing.</summary>
public class MapRendererTests
{
    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    /// <summary>Verifies that terrain-only renders produce byte-identical output to <see cref="TerrainImage.ToRgb24"/>.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Render_terrain_only_matches_the_verified_terrain_image()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var map = MapDocument.Load("LVL000", loader);
        Assert.NotNull(map);

        var options = new MapRenderOptions(ShowTerrain: true, ShowCollision: false, ShowExitInfo: false,
            ShowBonusInfo: false, VisibleEntityKeys: new HashSet<(byte, ushort?)>(),
            VisibleColliderKeys: new HashSet<(byte, ushort?)>());
        var result = MapRenderer.Render(map!, options, loader, _ => false);

        Assert.NotNull(result);
        Assert.True(result!.ShowedTerrain);
        Assert.False(result.ShowedCollision);
        Assert.False(result.ShowedEntities);
        Assert.Equal(map!.Terrain.Width, result.Width);
        Assert.Equal(map.Terrain.Height, result.Height);

        var actual = Convert.ToHexString(SHA256.HashData(result.Rgb)).ToLowerInvariant();
        var expected = Convert.ToHexString(SHA256.HashData(map.Terrain.ToRgb24())).ToLowerInvariant();
        Assert.Equal(expected, actual);

        // Terrain is an opaque layer: its palette index 0 is the level's own black, not a hole.
        Assert.All(result.Alpha, coverage => Assert.Equal(255, coverage));
    }

    /// <summary>Verifies that hiding layers keeps the full map dimensions rather than cropping to what is left.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Render_entities_only_keeps_the_full_map_dimensions()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var map = MapDocument.Load("LVL000", loader);
        Assert.NotNull(map);
        Assert.NotEmpty(map!.Entities);

        var allKeys = map.Entities.Select(EntityCategories.KeyOf).ToHashSet();
        var options = new MapRenderOptions(ShowTerrain: false, ShowCollision: false, ShowExitInfo: false,
            ShowBonusInfo: false, VisibleEntityKeys: allKeys, VisibleColliderKeys: allKeys);
        var result = MapRenderer.Render(map, options, loader, _ => false);

        Assert.NotNull(result);
        Assert.Equal(map.Terrain.Width, result!.Width);
        Assert.Equal(map.Terrain.Height, result.Height);
        Assert.Equal(result.Width * result.Height, result.Alpha.Length);
        Assert.Contains<byte>(0, result.Alpha); // the sprites cover nothing like the whole playfield
    }

    /// <summary>Verifies that collision overlays alpha-blend material color tints over underlying terrain pixels.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Render_with_collision_blends_the_material_overlay()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var map = MapDocument.Load("LVL000", loader);
        Assert.NotNull(map);
        Assert.NotNull(map!.Collision); // LVL000 ships an M.MPF

        var collision = map.Collision!;
        var terrain = map.Terrain;
        int w = Math.Min(terrain.Width, collision.Width);
        int h = Math.Min(terrain.Height, collision.Height);

        // Find a pixel whose material overlay is non-transparent, so the blend is actually observable.
        int foundX = -1, foundY = -1;
        for (int y = 0; y < h && foundX < 0; y++)
        for (int x = 0; x < w; x++)
        {
            byte code = collision.MaterialCodes[y * collision.Width + x];
            if (MaterialPalette.OverlayColorOf(code).A > 0) { foundX = x; foundY = y; break; }
        }
        Assert.True(foundX >= 0, "test assumption: LVL000's collision has at least one non-transparent material pixel");

        var options = new MapRenderOptions(ShowTerrain: true, ShowCollision: true, ShowExitInfo: false,
            ShowBonusInfo: false, VisibleEntityKeys: new HashSet<(byte, ushort?)>(),
            VisibleColliderKeys: new HashSet<(byte, ushort?)>());
        var result = MapRenderer.Render(map, options, loader, _ => false);
        Assert.NotNull(result);
        Assert.True(result!.ShowedCollision);

        int tp = terrain.Indices[foundY * terrain.Width + foundX] * 3;
        var pal = terrain.Palette.Rgb;
        byte tr = pal[tp], tg = pal[tp + 1], tb = pal[tp + 2];
        byte code2 = collision.MaterialCodes[foundY * collision.Width + foundX];
        var (cr, cg, cb, ca) = MaterialPalette.OverlayColorOf(code2);
        byte expectedR = (byte)((cr * ca + tr * (255 - ca)) / 255);
        byte expectedG = (byte)((cg * ca + tg * (255 - ca)) / 255);
        byte expectedB = (byte)((cb * ca + tb * (255 - ca)) / 255);

        int o = (foundY * w + foundX) * 3;
        Assert.Equal((expectedR, expectedG, expectedB), (result.Rgb[o], result.Rgb[o + 1], result.Rgb[o + 2]));
    }

    /// <summary>Verifies that entity-only renders leave the canvas around the sprites uncovered.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Render_entities_only_leaves_the_base_canvas_uncovered()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var map = MapDocument.Load("LVL000", loader);
        Assert.NotNull(map);
        Assert.NotEmpty(map!.Entities);

        var allKeys = map.Entities.Select(EntityCategories.KeyOf).ToHashSet();
        var options = new MapRenderOptions(ShowTerrain: false, ShowCollision: false, ShowExitInfo: false,
            ShowBonusInfo: false, VisibleEntityKeys: allKeys, VisibleColliderKeys: allKeys);
        var result = MapRenderer.Render(map, options, loader, _ => false);

        Assert.NotNull(result);
        Assert.False(result!.ShowedTerrain);
        Assert.True(result.ShowedEntities);
        Assert.Equal(0, result.Alpha[0]); // pixel (0,0), outside every sprite
        Assert.Contains<byte>(255, result.Alpha); // the sprites themselves
    }

    /// <summary>Verifies that flat entity markers render a 1px white anchor pixel at exact entity coordinates.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Render_entity_marker_has_a_white_anchor_pixel()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        Func<string, byte[]?> noRealPaletteLoader = name =>
            name.EndsWith(".PAL", StringComparison.OrdinalIgnoreCase) ? null : loader(name);

        var map = MapDocument.Load("LVL000", noRealPaletteLoader);
        Assert.NotNull(map);
        Assert.Null(map!.RealPalette);
        Assert.NotEmpty(map.Entities);

        var rec = map.Entities[0];
        var options = new MapRenderOptions(ShowTerrain: false, ShowCollision: false, ShowExitInfo: false,
            ShowBonusInfo: false, VisibleEntityKeys: new HashSet<(byte, ushort?)> { EntityCategories.KeyOf(rec) },
            VisibleColliderKeys: new HashSet<(byte, ushort?)>());
        var result = MapRenderer.Render(map, options, noRealPaletteLoader, _ => false);

        Assert.NotNull(result);
        Assert.True(rec.X >= 0 && rec.X < result!.Width && rec.Y >= 0 && rec.Y < result.Height,
            "test assumption: the first entity's anchor is within the canvas");
        int o = (rec.Y * result.Width + rec.X) * 3;
        Assert.Equal((255, 255, 255), (result.Rgb[o], result.Rgb[o + 1], result.Rgb[o + 2]));
    }

    /// <summary>Renders a synthetic Dig Spot exit to <paramref name="slot"/> on LVL000 with optional font glyphs.</summary>
    private static MapRenderResult RenderSyntheticExit(Func<string, byte[]?> loader, GameFont? font, int slot)
    {
        var map = MapDocument.Load("LVL000", loader);
        Assert.NotNull(map);

        // A synthetic Dig Spot exit (Category 0x05, Type 0): real DLF content varies per level, so this
        // is a controlled placement rather than depending on LVL000 actually shipping one.
        var exitRecord = new DlfRecord(Category: 0x05, Type: 0, X: 100, Y: 100, P0: (ushort)slot,
            P1: 0, P2: 0, P3: 0, P4: 0);
        var syntheticMap = map! with { Entities = [exitRecord] };

        var options = new MapRenderOptions(ShowTerrain: true, ShowCollision: false, ShowExitInfo: true,
            ShowBonusInfo: false, VisibleEntityKeys: new HashSet<(byte, ushort?)>(),
            VisibleColliderKeys: new HashSet<(byte, ushort?)>());
        var result = MapRenderer.Render(syntheticMap, options, loader, _ => false, font);
        Assert.NotNull(result);
        return result!;
    }

    // Mirrors the ramp styles MapRenderer draws exit labels in. They are private there, so the tests restate
    // them; the assertions below fail loudly if the two ever drift apart, since no other ramp's tones match.
    private const int MapExitRampStyle = 2;
    private const int LevelExitRampStyle = 5;
    private const int DeadEndRampStyle = 1;

    // The slot value a Dig Spot carries when its exit ends the level at the world map.
    private const int WorldMapSlot = 65535;

    private static readonly (byte, byte, byte) OutlineBlack = (0, 0, 0);

    /// <summary>Every color <paramref name="style"/>'s ramp resolves to under LVL000's own palette, which
    /// is the palette the render draws that level's labels under.</summary>
    private static HashSet<(byte, byte, byte)> RampTones(Func<string, byte[]?> loader, int style)
    {
        var palette = VgaPalette.From6Bit(loader("LVL000.PAL")!.AsSpan(0, 768));
        return GameFont.Ramp(style).Where(i => i != 0)
            .Select(i => (palette.Rgb[i * 3], palette.Rgb[i * 3 + 1], palette.Rgb[i * 3 + 2]))
            .ToHashSet();
    }

    /// <summary>Extracts distinct RGB colors introduced by the exit-info label relative to a font-less render.</summary>
    private static HashSet<(byte, byte, byte)> LabelColors(Func<string, byte[]?> loader, GameFont font, int slot)
    {
        var withFont = RenderSyntheticExit(loader, font, slot);
        var withoutFont = RenderSyntheticExit(loader, font: null, slot);

        var colors = new HashSet<(byte, byte, byte)>();
        for (int i = 0; i < withFont.Rgb.Length; i += 3)
        {
            if (withFont.Rgb[i] == withoutFont.Rgb[i]
                && withFont.Rgb[i + 1] == withoutFont.Rgb[i + 1]
                && withFont.Rgb[i + 2] == withoutFont.Rgb[i + 2]) continue;
            colors.Add((withFont.Rgb[i], withFont.Rgb[i + 1], withFont.Rgb[i + 2]));
        }
        return colors;
    }

    /// <summary>Verifies that exit labels for markers with negative coordinates (e.g. LVL209 drain at x = -30) are clamped within canvas bounds.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void A_label_whose_marker_hangs_off_the_level_edge_is_moved_fully_inside()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var font = GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));

        var map = MapDocument.Load("LVL209", loader);
        Assert.NotNull(map);
        Assert.Contains(map!.Entities, e => e.Category == 0x5B && e.X < 0);

        var options = new MapRenderOptions(ShowTerrain: true, ShowCollision: false, ShowExitInfo: true,
            ShowBonusInfo: true, VisibleEntityKeys: new HashSet<(byte, ushort?)>(),
            VisibleColliderKeys: new HashSet<(byte, ushort?)>());
        var withFont = MapRenderer.Render(map, options, loader, _ => false, font);
        var withoutFont = MapRenderer.Render(map, options, loader, _ => false, font: null);
        Assert.NotNull(withFont);
        Assert.NotNull(withoutFont);

        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (int y = 0; y < withFont!.Height; y++)
        {
            for (int x = 0; x < withFont.Width; x++)
            {
                int o = (y * withFont.Width + x) * 3;
                if (withFont.Rgb[o] == withoutFont!.Rgb[o]
                    && withFont.Rgb[o + 1] == withoutFont.Rgb[o + 1]
                    && withFont.Rgb[o + 2] == withoutFont.Rgb[o + 2]) continue;
                var px = (withFont.Rgb[o], withFont.Rgb[o + 1], withFont.Rgb[o + 2]);
                if (px == OutlineBlack) continue;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }
        }

        Assert.True(maxX >= 0, "test assumption: LVL209 draws a destination-info label at all");
        Assert.True(minX >= 1 && minY >= 1, $"ink starts at ({minX},{minY}), with no room for its outline");
        Assert.True(maxX <= withFont.Width - 2 && maxY <= withFont.Height - 2,
            $"ink ends at ({maxX},{maxY}) on a {withFont.Width}x{withFont.Height} canvas");
    }

    /// <summary>Verifies that collider footprint layers render independently of sprite visibility layers.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void A_footprint_answers_to_its_own_layer_and_not_to_its_sprites()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var map = MapDocument.Load("LVL200", loader); // water: its drain is the level's only collider owner
        Assert.NotNull(map?.Collision);
        Assert.NotEmpty(map!.ColliderKeys);

        var allKeys = map.Entities.Select(EntityCategories.KeyOf).ToHashSet();
        var spriteHidden = allKeys.Where(k => !map.ColliderKeys.Contains(k)).ToHashSet();
        Assert.NotEqual(allKeys.Count, spriteHidden.Count);
        var footprints = map.ColliderKeys.ToHashSet();
        var noFootprintTicked = new HashSet<(byte, ushort?)>();

        Assert.NotEqual(Render(map, spriteHidden, loader, footprints),
            Render(map, spriteHidden, loader, noFootprintTicked));
        Assert.Equal(Render(map with { ColliderStamps = [] }, spriteHidden, loader, footprints),
            Render(map, spriteHidden, loader, noFootprintTicked));
    }

    /// <summary>Verifies that collider footprints can render onto an open plane when the base collision layer is hidden.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void A_footprint_draws_without_the_levels_own_collision_plane()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var map = MapDocument.Load("LVL200", loader);
        Assert.NotNull(map?.Collision);
        var footprints = map!.ColliderKeys.ToHashSet();
        Assert.NotEmpty(footprints);
        var noEntities = new HashSet<(byte, ushort?)>();

        var options = new MapRenderOptions(ShowTerrain: true, ShowCollision: false, ShowExitInfo: false,
            ShowBonusInfo: false, VisibleEntityKeys: noEntities, VisibleColliderKeys: footprints);
        var result = MapRenderer.Render(map, options, loader, _ => false);
        Assert.NotNull(result);
        Assert.True(result!.ShowedCollision);

        Assert.NotEqual(Render(map, noEntities, loader, footprints, showCollisionPlane: false),
            Render(map, noEntities, loader, noEntities, showCollisionPlane: false));
        Assert.NotEqual(Render(map, noEntities, loader, footprints, showCollisionPlane: false),
            Render(map, noEntities, loader, footprints, showCollisionPlane: true));
    }

    /// <summary>Verifies that moving platforms and falling platforms are isolated into independent layers.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Hiding_one_platform_kind_leaves_the_other_kind_whole()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var map = MapDocument.Load("LVL092", loader); // caves: one of the six levels carrying both kinds
        Assert.NotNull(map?.Collision);

        (byte, ushort?) moving = (0x04, 0), falling = (0x04, 1);
        var keys = map!.Entities.Select(EntityCategories.KeyOf).ToHashSet();
        Assert.Contains(moving, keys);
        Assert.Contains(falling, keys);
        Assert.Contains(map.ColliderStamps, s => s.LayerKey == falling);

        var movingOnly = new HashSet<(byte, ushort?)> { moving };
        var noFallingAtAll = map with
        {
            Entities = [.. map.Entities.Where(e => EntityCategories.KeyOf(e) != falling)],
            ColliderStamps = [.. map.ColliderStamps.Where(s => s.LayerKey != falling)],
        };

        Assert.Equal(Render(noFallingAtAll, movingOnly, loader), Render(map, movingOnly, loader));
        Assert.NotEqual(Render(map, keys, loader), Render(map, movingOnly, loader));
    }

    private static string Render(MapDocument map, HashSet<(byte, ushort?)> keys, Func<string, byte[]?> loader,
        HashSet<(byte, ushort?)>? colliderKeys = null, bool showCollisionPlane = true)
    {
        var options = new MapRenderOptions(ShowTerrain: true, ShowCollision: showCollisionPlane,
            ShowExitInfo: false,
            ShowBonusInfo: false, VisibleEntityKeys: keys, VisibleColliderKeys: colliderKeys ?? keys);
        var result = MapRenderer.Render(map, options, loader, _ => false);
        Assert.NotNull(result);
        return Convert.ToHexString(SHA256.HashData(result!.Rgb));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Render_exit_info_draws_independently_of_marker_visibility_when_a_font_is_available()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var font = GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));

        Assert.NotEmpty(LabelColors(loader, font, WorldMapSlot));
    }

    /// <summary>Verifies that exit labels for markers with negative coordinates (e.g. LVL209 drain at x = -30) are clamped within canvas bounds.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Exit_info_labels_keep_the_ramps_shading_rather_than_one_flat_color()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var font = GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));

        var inkTones = LabelColors(loader, font, WorldMapSlot).Intersect(RampTones(loader, MapExitRampStyle)).ToList();

        Assert.True(inkTones.Count > 1,
            $"expected several ramp tones across the glyphs, found {inkTones.Count}");
    }

    /// <summary>Verifies that exit info labels are rendered strictly using ramp tones and 1px black outlines.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Exit_info_labels_carry_the_games_black_outline_and_nothing_else()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var font = GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));

        var added = LabelColors(loader, font, WorldMapSlot);
        var rampTones = RampTones(loader, MapExitRampStyle);

        Assert.Contains(OutlineBlack, added);
        Assert.All(added, px => Assert.True(px == OutlineBlack || rampTones.Contains(px),
            $"{px} is neither a ramp tone nor the outline's black"));
    }

    /// <summary>Verifies that an exit ending at the world map, one into another level and one that leads
    /// back into this level draw in ramps of their own, with no tone in common.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Each_kind_of_exit_reads_apart_from_the_others()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var font = GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));

        var toMap = LabelColors(loader, font, WorldMapSlot).Where(px => px != OutlineBlack).ToHashSet();
        var toLevel = LabelColors(loader, font, slot: 3).Where(px => px != OutlineBlack).ToHashSet();
        // LVL000's own slot, so this one is the Spookstone shape: a door back into the level it stands in.
        var backHere = LabelColors(loader, font, slot: 0).Where(px => px != OutlineBlack).ToHashSet();

        Assert.NotEmpty(toMap);
        Assert.NotEmpty(toLevel);
        Assert.NotEmpty(backHere);
        Assert.Subset(RampTones(loader, MapExitRampStyle), toMap);
        Assert.Subset(RampTones(loader, LevelExitRampStyle), toLevel);
        Assert.Subset(RampTones(loader, DeadEndRampStyle), backHere);
        Assert.Empty(toMap.Intersect(toLevel));
        Assert.Empty(toMap.Intersect(backHere));
        Assert.Empty(toLevel.Intersect(backHere));
    }

    /// <summary>Verifies that exit info overlay rendering is safely omitted when no font is available.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Render_exit_info_draws_nothing_without_a_font()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var result = RenderSyntheticExit(loader, font: null, WorldMapSlot);
        var rampTones = RampTones(loader, MapExitRampStyle);

        for (int i = 0; i < result.Rgb.Length; i += 3)
        {
            Assert.DoesNotContain((result.Rgb[i], result.Rgb[i + 1], result.Rgb[i + 2]), rampTones);
        }
    }
}
