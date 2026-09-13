using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Tests;

/// <summary>Pins Silver Goodies animation tables and rendered frame hashes against reference files.</summary>
public class SilverItemsAnimationTests
{
    private static readonly (string Name, string Label, FrameRect Rect)[] Expected =
    [
        ("energy", "Energy", new FrameRect(0, 1, 32, 20, 27)),
        ("spring_boots", "Spring Boots", new FrameRect(0, 22, 32, 20, 20)),
        ("rapid_fire", "Rapid Fire", new FrameRect(0, 43, 32, 20, 20)),
        ("jetpack", "Jet Pack", new FrameRect(0, 64, 32, 20, 20)),
        ("time", "Time", new FrameRect(0, 85, 32, 20, 20)),
        ("super_dug", "Super Dug", new FrameRect(0, 106, 32, 20, 20)),
        ("trail_mark", "Trail Mark", new FrameRect(0, 127, 32, 20, 20)),
    ];

    private static readonly string[] ExpectedShas =
    [
        "DA069BCDF7F99D67DB44E6514151E9E2B28B1D36A456B0B88304DCADDC8C7D1C",
        "B35AC74E9F458BA2178A67FE18D18DCB22675F1D253A6C8AE6D27B55CE265D87",
        "24FE82BDED6179AA3470BB2CD9897C605FCE4DC205C6338FE857985B719BEF25",
        "9FD2610303961FA6A7E3EA21D06AA062E63E4F490B2359E10BEC19A6134F92A9",
        "62666AAE4F15E1893CECCB1F4A3DCEA1CBE3F8C2609275FBFB8DC3A5C7B7B103",
        "941B223F87414C7701F647580B5E7C337E7B389ACF927813BB425EC6F1492080",
        "69762E971F1E56DD71A8DBF2A9C35C8D1C98758B1A1839D2BD4696F6937B031A",
    ];

    /// <summary>Verifies that Silver Goodies frames carry the six-cell offset on the shared sheet.</summary>
    [Fact]
    public void SilverItems_table_matches_reference_transcription()
    {
        var set = AnimationTables.SilverItems;
        Assert.Equal("Silver Goodies", set.Name);
        Assert.Equal("WO_G&S", set.FixedSheet);
        Assert.Equal("LVL000.PAL", set.FixedPalette);
        Assert.False(set.UseEmbeddedPalette);
        Assert.Null(set.Category);
        Assert.Equal(Expected.Length, set.Anims.Count);
        Assert.True(set.SlicedSheet);
        Assert.Null(set.Rects);

        for (int i = 0; i < Expected.Length; i++)
        {
            Assert.Equal(Expected[i].Name, set.Anims[i].Name);
            Assert.Equal([6 + i], set.Anims[i].Frames);
            Assert.Equal(AnimMode.Pose, set.Anims[i].Mode);
            Assert.Equal(Expected[i].Label, SkinCatalog.PrettyAnimName(set.Name, set.Anims[i]));
        }
    }

    /// <summary>Guards that the tree and the Layers panel take a silver goodie's name from one table.</summary>
    [Fact]
    public void SilverItems_labels_are_the_shared_subtype_names()
    {
        var set = AnimationTables.SilverItems;
        for (int type = 0; type < set.Anims.Count; type++)
            Assert.Equal(EntityCategories.SubtypeNameOf(0x01, (ushort)type),
                SkinCatalog.PrettyAnimName(set.Name, set.Anims[type]));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void SilverItems_rects_derived_from_the_sheet_are_the_silver_row()
    {
        using var library = ReadSheet(out var sheet);

        var rects = SkinCatalog.RectsOf(AnimationTables.SilverItems, sheet.Frames)!;

        Assert.Equal(Expected.Length, rects.Count);
        for (int i = 0; i < Expected.Length; i++)
            Assert.Equal(Expected[i].Rect, rects[6 + i]);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void SilverItems_frame_pixels_match_reference_pixels()
    {
        using var library = ReadSheet(out var sheet);

        var set = AnimationTables.SilverItems;
        var rects = SkinCatalog.RectsOf(set, sheet.Frames)!;
        var palette = VgaPalette.From6Bit(
            library.Read(set.FixedPalette!).AsSpan(0, 768));

        for (int i = 0; i < Expected.Length; i++)
        {
            var rect = rects[6 + i];
            var cell = SpriteSheetSlicer.RectCell(sheet.Frames[rect.Page], rect.X, rect.Y,
                rect.X + rect.W - 1, rect.Y + rect.H - 1);
            Assert.Equal(ExpectedShas[i], AnimationPixels.Sha256Rgba(cell, palette), ignoreCase: true);
        }
    }

    /// <summary>Guards that Gold and Silver item frame sets remain disjoint on the shared sheet.</summary>
    [Fact]
    public void The_two_goodie_rows_do_not_share_a_cell()
    {
        var gold = AnimationTables.GoldItems.Anims.SelectMany(a => a.Frames).ToHashSet();
        var silver = AnimationTables.SilverItems.Anims.SelectMany(a => a.Frames).ToHashSet();

        Assert.Empty(gold.Intersect(silver));
        Assert.Equal(Enumerable.Range(0, 13), gold.Concat(silver).OrderBy(f => f));
    }

    private static ResourceLibrary ReadSheet(out SheetImage sheet)
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains("WO_G&S.SPF"), "WO_G&S.SPF is missing from this install");

        sheet = SheetImage.Read(library.Read("WO_G&S.SPF"));
        return library;
    }
}
