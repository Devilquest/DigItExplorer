using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Tests;

/// <summary>Pins Gold Goodies animation tables and rendered frame hashes against reference files.</summary>
public class GoldItemsAnimationTests
{
    private static readonly (string Name, string Label, FrameRect Rect)[] Expected =
    [
        ("extra_dug", "Extra Dug", new FrameRect(0, 1, 1, 20, 27)),
        ("shovel", "Shovel", new FrameRect(0, 22, 1, 20, 20)),
        ("pickaxe", "Pickaxe", new FrameRect(0, 43, 1, 20, 20)),
        ("diving_helmet", "Diving Helmet", new FrameRect(0, 64, 1, 20, 20)),
        ("gold_goodie_unused", "Gold Goodie (unused)", new FrameRect(0, 85, 1, 20, 20)),
        ("torch", "Torch", new FrameRect(0, 106, 1, 20, 20)),
    ];

    private static readonly string[] ExpectedShas =
    [
        "8D8B7F32F303C64DEACD92C2F30485985E3722A04E1628515235A3E92BB062B3",
        "30B32431DEE9B2EAE06BCD4BB10E954B099615E5809247B427D6906BB35EBBAC",
        "3F5FBC0053DA7D46B0C639553809900412C6B1287EA8AC733CF2BA53DCFEFEF8",
        "C8E13D3D1B7D539680D230DE393921C8F9137C81D07C5DDFD191B9A054D4DCC8",
        "787CF8ECCF0494AF3AD28F6553DC06B715994977A75E446F3BA081DE813B9290",
        "D8441DA69882B7D341E47B6263B743F1DFB8CBDA1DADB558063064FD49B5ADAC",
    ];

    [Fact]
    public void GoldItems_table_matches_reference_transcription()
    {
        var set = AnimationTables.GoldItems;
        Assert.Equal("Gold Goodies", set.Name);
        Assert.Equal("WO_G&S", set.FixedSheet);
        Assert.Equal("LVL000.PAL", set.FixedPalette);
        Assert.False(set.UseEmbeddedPalette);
        Assert.Null(set.Category);
        Assert.Equal(Expected.Length, set.Anims.Count);
        Assert.True(set.SlicedSheet);
        Assert.Null(set.Rects);

        for (int i = 0; i < Expected.Length; i++)
        {
            var (name, label, _) = Expected[i];
            Assert.Equal(name, set.Anims[i].Name);
            Assert.Equal([i], set.Anims[i].Frames);
            Assert.Equal(AnimMode.Pose, set.Anims[i].Mode);
            Assert.Equal(label, SkinCatalog.PrettyAnimName(set.Name, set.Anims[i]));
        }
    }

    /// <summary>Guards that the tree and the Layers panel take a gold goodie's name from one table.</summary>
    [Fact]
    public void GoldItems_labels_are_the_shared_subtype_names()
    {
        var set = AnimationTables.GoldItems;
        for (int type = 0; type < set.Anims.Count; type++)
            Assert.Equal(EntityCategories.SubtypeNameOf(0x00, (ushort)type),
                SkinCatalog.PrettyAnimName(set.Name, set.Anims[type]));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void GoldItems_rects_derived_from_the_sheet_are_the_gold_row()
    {
        using var library = ReadSheet(out var sheet);

        var rects = SkinCatalog.RectsOf(AnimationTables.GoldItems, sheet.Frames);

        // Only the frames the table animates are kept: cells 6 and up are the silver row and the caption
        // strips, and must not reach the player's stage sizing.
        Assert.Equal(Expected.Length, rects!.Count);
        for (int i = 0; i < Expected.Length; i++)
            Assert.Equal(Expected[i].Rect, rects[i]);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void GoldItems_frame_pixels_match_reference_pixels()
    {
        using var library = ReadSheet(out var sheet);

        var set = AnimationTables.GoldItems;
        var rects = SkinCatalog.RectsOf(set, sheet.Frames)!;
        var palette = VgaPalette.From6Bit(
            library.Read(set.FixedPalette!).AsSpan(0, 768));

        for (int i = 0; i < Expected.Length; i++)
        {
            var rect = rects[i];
            var cell = SpriteSheetSlicer.RectCell(sheet.Frames[rect.Page], rect.X, rect.Y,
                rect.X + rect.W - 1, rect.Y + rect.H - 1);
            Assert.Equal(ExpectedShas[i], AnimationPixels.Sha256Rgba(cell, palette), ignoreCase: true);
        }
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
