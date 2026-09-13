using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins standalone effect and projectile animation tables against reference game files.</summary>
public class MiscEffectsAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedFireball =
    [
        ("flight_intro", [0, 1, 2, 3, 4, 5, 6, 7, 8], AnimMode.Once),
        ("flight_loop", [9, 10, 11, 12], AnimMode.Loop),
        ("impact", [13, 14, 15, 16, 17, 18, 19], AnimMode.Once),
    ];

    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedGeneral =
    [
        ("bullets", [0, 1, 2, 3], AnimMode.Loop),
        ("jetpack_smoke", [4, 5, 6, 7], AnimMode.Loop),
        ("wall_hit_earth", [8, 9, 10, 11, 12, 13], AnimMode.Once),
        ("wall_hit_gray", [14, 15, 16, 17, 18, 19], AnimMode.Once),
        ("jump_dirt", [20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
                       30, 31, 32, 33, 34, 35, 36, 37, 38, 39], AnimMode.Once),
    ];

    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedHit =
    [
        ("hit", [0, 1, 2, 3, 4], AnimMode.Once),
    ];

    // One SHA-256 per animation over its concatenated RGBA frames, row-major with no row padding.
    private static readonly string[] FireballShas =
    [
        "9E26E82A051E3736DFF1738E14303D3A469C3E2BC0531B363CC99778ED0FC93A", // flight_intro
        "C5846901372C2D453F0E38C94F4933D11E28ABE13267958C438BD7D47F7F3907", // flight_loop
        "248DA68FB5440A72F09D7E9BD5094E48CEF8F4CA962D5D0919AECEE02902C0F6", // impact
    ];

    private static readonly string[] GeneralShas =
    [
        "EA3EB19F99827A7B62D69F7D998B6B95D9934B9AD615010DFD5934E6ABF1D0F4", // bullets
        "781CC6CA7241F7110333A4EA98B800709FAEBB47F8D31831259C4E8BF324DB3B", // jetpack_smoke
        "3A3E7F932B86EF190C488535269673D7AD41A8FAA51CE82C223786CD35332000", // wall_hit_earth
        "425DC9961F94EF61343B6D398578FC98811BDD4323579E70EB7BE17D9F040DF3", // wall_hit_gray
        "7B967933EE2E2DFE01A7E2BF3D7AC08F86B23E6D53DA4A656E64BB0939B1DAEB", // jump_dirt
    ];

    private static readonly string[] HitShas =
    [
        "1FFED145CA318020416F2AE5667699EC936BADD1F74E23FF1738666D84D1E998", // hit
    ];

    [Theory]
    [InlineData("Fireball")]
    [InlineData("General Effects")]
    [InlineData("Hit")]
    public void Misc_sets_have_no_category_and_a_single_fixed_sheet(string name)
    {
        var set = AnimationTables.All[name];
        // No DLF category is what keeps these out of the Enemies branch; a FixedSheet (rather than a world
        // suffix or a costume letter) is what makes them a single-skin entry in the tree.
        Assert.Null(set.Category);
        Assert.Null(set.SheetLetter);
        Assert.NotNull(set.FixedSheet);
        // Geometry comes from exactly one of the two sources, never both and never neither: a compiled-in
        // rect table for the sheets that carry no separator lines, the sheet itself for the ones that do.
        Assert.True(set.Rects is not null ^ set.SlicedSheet);
    }

    [Fact]
    public void Fireball_table_matches_reference_transcription()
    {
        var set = AnimationTables.Fireball;
        Assert.Equal("Fireball", set.Name);
        Assert.Equal("WO_FBALL", set.FixedSheet);
        AssertTable(ExpectedFireball, set);

        // A single row of 20 cells at y=164, x = i*16: the sheet's whole content.
        Assert.Equal(20, set.Rects!.Count);
        Assert.Equal(new FrameRect(0, 0, 164, 15, 13), set.Rects[0]);
        Assert.Equal(new FrameRect(0, 19 * 16, 164, 15, 13), set.Rects[19]);
    }

    [Fact]
    public void GeneralEffects_table_matches_reference_transcription()
    {
        var set = AnimationTables.GeneralEffects;
        Assert.Equal("General Effects", set.Name);
        Assert.Equal("GENERAL", set.FixedSheet);
        AssertTable(ExpectedGeneral, set);

        // Four unrelated pieces at four cell sizes, each registered at its own sprite base.
        Assert.Equal(40, set.Rects!.Count);
        Assert.Equal(new FrameRect(0, 31, 0, 10, 5), set.Rects[0]);          // bullets, base 2
        Assert.Equal(new FrameRect(0, 90, 0, 5, 5), set.Rects[7]);           // jetpack smoke, base 90
        Assert.Equal(new FrameRect(0, 102, 0, 14, 11), set.Rects[8]);        // wall hit earth, base 6
        Assert.Equal(new FrameRect(0, 102, 12, 14, 11), set.Rects[14]);      // wall hit gray, same x, y+12
        Assert.Equal(new FrameRect(0, 19 * 16, 191, 16, 8), set.Rects[39]);  // jump dirt, base 220
    }

    [Fact]
    public void Hit_table_matches_reference_transcription()
    {
        var set = AnimationTables.Hit;
        Assert.Equal("Hit", set.Name);
        Assert.Equal("WO_BONK", set.FixedSheet);
        AssertTable(ExpectedHit, set);

        // WO_BONK is separator-packed: the five 32×29 star cells are read from the sheet, not listed here.
        Assert.True(set.SlicedSheet);
        Assert.Null(set.Rects);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Hit_rects_derived_from_the_sheet_are_the_five_star_cells()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var pages = SheetImage.Read(library.Read("WO_BONK.SPF")).Frames;
        var rects = SkinCatalog.RectsOf(AnimationTables.Hit, pages);

        Assert.Equal(5, rects!.Count);
        Assert.Equal(new FrameRect(0, 0, 1, 32, 29), rects[0]);
        Assert.Equal(new FrameRect(0, 4 * 33, 1, 32, 29), rects[4]);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.SupportedBuild)]
    [InlineData("Fireball", "WO_FBALL.SPF")]
    [InlineData("General Effects", "GENERAL.SPF")]
    [InlineData("Hit", "WO_BONK.SPF")]
    public void Misc_sheets_ship_unsuffixed_only(string name, string sheetFile)
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var stem = AnimationTables.All[name].FixedSheet!;
        Assert.True(library.Contains(sheetFile));
        foreach (var suffix in new[] { "00", "02", "03" })
        {
            Assert.False(library.Contains($"{stem}{suffix}.SPF"));
            Assert.False(library.Contains($"{stem}{suffix}.MPF"));
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.FullResourceSet)]
    [InlineData("Fireball", "WO_FBALL.SPF")]
    [InlineData("General Effects", "GENERAL.SPF")]
    [InlineData("Hit", "WO_BONK.SPF")]
    public void Misc_frame_pixels_match_reference_pixels(string name, string sheetFile)
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.All[name];
        var shas = name switch
        {
            "Fireball" => FireballShas,
            "General Effects" => GeneralShas,
            _ => HitShas,
        };

        var pages = SheetImage.Read(library.Read(sheetFile)).Frames;
        var palette = VgaPalette.From6Bit(
            library.Read(set.FixedPalette!).AsSpan(0, 768));

        var cells = new Dictionary<int, SpriteCell>();
        foreach (var (frameId, rect) in SkinCatalog.RectsOf(set, pages)!)
            cells[frameId] = SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y,
                rect.X + rect.W - 1, rect.Y + rect.H - 1);

        for (int i = 0; i < set.Anims.Count; i++)
        {
            bool absent = set.Anims[i].Frames.All(f => cells[f].IsEmpty);
            Assert.False(absent, $"{name}/{set.Anims[i].Name}: expected art, found all-empty cells");

            using var sha = SHA256.Create();
            foreach (var f in set.Anims[i].Frames)
            {
                var cell = cells[f];
                var rgba = new byte[cell.W * cell.H * 4];
                AnimationPixels.ToRgba(cell, palette, rgba);
                sha.TransformBlock(rgba, 0, rgba.Length, null, 0);
            }
            sha.TransformFinalBlock([], 0, 0);
            Assert.Equal(shas[i], Convert.ToHexString(sha.Hash!), ignoreCase: true);
        }
    }

    /// <summary>Pinning the fireball's palette is a consistency choice with the other effect sheets rather
    /// than a color change.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Fireball_pixels_are_palette_independent_between_caves_and_the_unsuffixed_default()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.Fireball;
        var pages = SheetImage.Read(library.Read("WO_FBALL.SPF")).Frames;
        var caves = VgaPalette.From6Bit(library.Read("LVL000.PAL").AsSpan(0, 768));
        var fallback = VgaPalette.From6Bit(
            library.Read(SkinCatalog.SuffixPal[""]).AsSpan(0, 768));

        foreach (var (_, rect) in set.Rects!)
        {
            var cell = SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y,
                rect.X + rect.W - 1, rect.Y + rect.H - 1);
            var a = new byte[cell.W * cell.H * 4];
            var b = new byte[cell.W * cell.H * 4];
            AnimationPixels.ToRgba(cell, caves, a);
            AnimationPixels.ToRgba(cell, fallback, b);
            Assert.Equal(a, b);
        }
    }

    private static void AssertTable((string Name, int[] Frames, AnimMode Mode)[] expected, CharacterAnimSet set)
    {
        Assert.Equal(expected.Length, set.Anims.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Name, set.Anims[i].Name);
            Assert.Equal(expected[i].Frames, set.Anims[i].Frames);
            Assert.Equal(expected[i].Mode, set.Anims[i].Mode);
        }
    }

    /// <summary>Verifies that all miscellaneous effect sets resolve valid sheets and non-empty animation frames.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.SupportedBuild)]
    [InlineData("Fireball", 1)]
    [InlineData("Fireball (unused)", 3)]
    [InlineData("General Effects", 1)]
    [InlineData("Hit", 1)]
    [InlineData("Sparkles", 1)]
    [InlineData("Flip It!", 1)]
    [InlineData("Spin It!", 1)]
    public void Misc_sets_resolve_a_sheet_and_report_art_for_every_animation(string name, int expectedSkins)
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(TestPaths.TryGetGameData(out var data));

        var catalog = new SkinCatalog(library.TryRead, data.Nodes, data.EntityNames);

        var set = AnimationTables.All[name];
        var skins = catalog.SkinsOf(set);
        Assert.Equal(expectedSkins, skins.Count);
        foreach (var skin in skins)
            for (int i = 0; i < set.Anims.Count; i++)
                Assert.True(skin.Present[i], $"{name}/{skin.Label}: {set.Anims[i].Name} reported as absent");
    }
}
