using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Tests;

/// <summary>Pins Gems animation table entries and sheet slice boundaries against reference files.</summary>
public class GemsAnimationTests
{
    private const int Count = 20;

    private static readonly int[] Unplaced = [6, 7, 9, 11, 19];

    /// <summary>Computes the expected frame rectangle for a gem index based on sheet stride.</summary>
    private static FrameRect ExpectedRect(int type) => new(0, 1 + 14 * type, 1, 13, 13);

    private static readonly string[] ExpectedShas =
    [
        "054F2E20F769CA8D7C81E92C173A9D7038BB35187698665AF683952FD70AA139",
        "5EF1EFB8C340F11CB20A79E637323EA0A77666D548C285054A49923F0DCA8EC4",
        "BBB6B953EE71CC88E01C06D8C69D0ABEE16F1A6E3BFD9ECDF9497EC9229EFDC9",
        "53787B0E9E1108C84EA740D71332AB5D4DC1AE9E22A0C3AEE52739C5DF50229C",
        "13A2EFF92028B3E13E0AF88884C263CF3A1344263843501D22566364F91696C2",
        "AAECC9D6220B077D0229FDF9587203380DE68802CBDB01302A06621604DA25EC",
        "0CE9C16AB8E1BBA851FA7A25ECC165384A5332F87C5C240E5BA1BE39BF5BAFF5",
        "E8BD6C9848B06FBC0C558F3510F07CB013911AFB339AE4FE1DFD7F2DAB1F437E",
        "F12FC84122596E57AB3C0FF4063777AA61D4150F7F6CCB050E3ECF7C2BC4EF98",
        "CAD7E5831A87440AA3034BA2358F1E4F835C4A7CC15BA56F11343E0FC31E31FA",
        "1680BC57EC6915523E6AF5880B913F38C79506A74F5666C7C467933539E5B256",
        "CDD3568913246D5943356B8614A7565DC1B10910768E985ADEB1A2A4ABD022AA",
        "52F02F4961C45D1141D85AEAF19BCD6F42203B5D35F1963488B5C732CD753598",
        "EA6EF69F0C4FE134850AD8BDD0328E15EF3C063E258A93AE56F405FF637B3EDE",
        "F1907F197C9415DA74F8E430F90324CBDA994614655373D61483C98C9CDD63E8",
        "FB0D3017EC887E0FF7DC9C51CA27E07366FAD2DC82ADDEC48EDF0E6392FD1E9C",
        "A0B216D6395D0B2E5704A31EC6F262B00FC766730D39919CF502A52513A8335C",
        "66100921DC1AF2FEF2C995CB8C27E9373EC4E1B5DBA84FE0108FFBF42ED96128",
        "1808ACB006967E5B8F46C420A87D5E41F26B695074A58BE002A96D84892E4B1A",
        "FD296F3FC3D270324F6FAFD9980223D75295DA1FB24272BCB2FBC51C5E32E36C",
    ];

    [Fact]
    public void Gems_table_matches_reference_transcription()
    {
        var set = AnimationTables.Gems;
        Assert.Equal("Gems", set.Name);
        Assert.Equal("WO_GEMS", set.FixedSheet);
        Assert.Equal("LVL000.PAL", set.FixedPalette);
        Assert.False(set.UseEmbeddedPalette);
        Assert.Null(set.Category);
        Assert.True(set.SlicedSheet);
        Assert.Null(set.Rects);
        Assert.Equal(Count, set.Anims.Count);

        for (int i = 0; i < Count; i++)
        {
            Assert.Equal([i], set.Anims[i].Frames);
            Assert.Equal(AnimMode.Pose, set.Anims[i].Mode);
        }
    }

    /// <summary>Guards that gems use index-based labels without invented names.</summary>
    [Fact]
    public void Gems_are_labeled_by_their_index_only()
    {
        var set = AnimationTables.Gems;

        Assert.Equal("Gem 0", SkinCatalog.PrettyAnimName(set.Name, set.Anims[0]));
        Assert.Equal("Gem 18", SkinCatalog.PrettyAnimName(set.Name, set.Anims[18]));
        for (int i = 0; i < Count; i++)
        {
            var expected = Unplaced.Contains(i) ? $"Gem {i} (unused)" : null;
            Assert.Equal(expected, set.Anims[i].DisplayName);
        }
    }

    /// <summary>Guards the five cells no shipped level places against a silent change of the set.</summary>
    [Fact]
    public void The_gems_no_level_places_are_marked_unused()
    {
        var set = AnimationTables.Gems;

        Assert.Equal([6, 7, 9, 11, 19], Unplaced);
        foreach (int type in Unplaced)
            Assert.Equal($"type {type}; sheet art exists but no shipped level spawns it",
                set.Anims[type].Note);
        foreach (var anim in set.Anims.Where(a => !Unplaced.Contains(a.Frames[0])))
            Assert.Equal($"type {anim.Frames[0]}", anim.Note);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Gems_rects_derived_from_the_sheet_are_the_gem_row()
    {
        using var library = ReadSheet(out var sheet);

        var rects = SkinCatalog.RectsOf(AnimationTables.Gems, sheet.Frames)!;

        Assert.Equal(Count, rects.Count);
        for (int i = 0; i < Count; i++)
            Assert.Equal(ExpectedRect(i), rects[i]);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Gems_frame_pixels_match_reference_pixels()
    {
        using var library = ReadSheet(out var sheet);

        var set = AnimationTables.Gems;
        var rects = SkinCatalog.RectsOf(set, sheet.Frames)!;
        var palette = VgaPalette.From6Bit(
            library.Read(set.FixedPalette!).AsSpan(0, 768));

        for (int i = 0; i < Count; i++)
        {
            var rect = rects[i];
            var cell = SpriteSheetSlicer.RectCell(sheet.Frames[rect.Page], rect.X, rect.Y,
                rect.X + rect.W - 1, rect.Y + rect.H - 1);
            Assert.Equal(ExpectedShas[i], AnimationPixels.Sha256Rgba(cell, palette), ignoreCase: true);
        }
    }

    /// <summary>Guards that the gem row and sparkle glints remain disjoint without overlapping cells.</summary>
    [Fact]
    public void The_gem_row_and_the_sparkle_glints_meet_without_overlapping()
    {
        var gems = AnimationTables.Gems.Anims.SelectMany(a => a.Frames).ToHashSet();
        var glints = AnimationTables.Sparkles.Anims.SelectMany(a => a.Frames).ToHashSet();

        Assert.Empty(gems.Intersect(glints));
        Assert.Equal(Count - 1, gems.Max());
        Assert.Equal(Count, glints.Min());
    }

    private static ResourceLibrary ReadSheet(out SheetImage sheet)
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains("WO_GEMS.SPF"), "WO_GEMS.SPF is missing from this install");

        sheet = SheetImage.Read(library.Read("WO_GEMS.SPF"));
        return library;
    }
}
