using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins minigame piece and prize icon rectangle tables against reference sheet files.</summary>
public class MinigamePiecesAnimationTests
{
    [Theory]
    [InlineData("Stop It! Pieces", "GM1_PCS", "Pieces")]
    [InlineData("Find It! Pieces", "GM3_PCS", "Pieces")]
    public void Pieces_sets_carry_the_five_row1_rects(string name, string sheet, string displayName)
    {
        var set = AnimationTables.All[name];
        Assert.Equal(sheet, set.FixedSheet);
        Assert.Equal(displayName, set.DisplayName);
        Assert.True(set.UseEmbeddedPalette);
        Assert.Equal(new HashSet<byte> { 1 }, set.TransparentIndices);
        Assert.False(set.SlicedSheet);
        Assert.Equal(5, set.Rects!.Count);

        for (int i = 0; i < 5; i++)
            Assert.Equal(new FrameRect(0, 1 + i * 43, 1, 42, 37), set.Rects[i]);
    }

    [Theory]
    [InlineData("Stop It! Prize Icons", "GM1_PCS", "Stop It! / Spin It!")]
    [InlineData("Find It! Prize Icons", "GM3_PCS", "Find It!")]
    public void PrizeIcons_sets_carry_the_two_row2_rects(string name, string sheet, string displayName)
    {
        var set = AnimationTables.All[name];
        Assert.Equal(sheet, set.FixedSheet);
        Assert.Equal(displayName, set.DisplayName);
        Assert.True(set.UseEmbeddedPalette);
        Assert.Equal(new HashSet<byte> { 1 }, set.TransparentIndices);
        Assert.False(set.SlicedSheet);
        Assert.Equal(2, set.Rects!.Count);

        for (int i = 0; i < 2; i++)
            Assert.Equal(new FrameRect(0, 1 + i * 30, 41, 29, 29), set.Rects[i]);
    }

    [Fact]
    public void FlipIt_prize_icons_read_page2_under_the_boards_own_palette()
    {
        var set = AnimationTables.FlipItPrizeIcons;
        Assert.Equal("GM0_TURN", set.FixedSheet);
        // The one prize-icon set that does not take an embedded palette: Flip It! is the only board that
        // ships a .PAL of its own, and the whole screen renders under it.
        Assert.False(set.UseEmbeddedPalette);
        Assert.Equal("GM0_PAL.PAL", set.FixedPalette);
        Assert.Equal("Flip It!", set.DisplayName);
        Assert.Equal(new HashSet<byte> { 1 }, set.TransparentIndices);
        Assert.False(set.SlicedSheet);
        Assert.Equal(2, set.Rects!.Count);
        Assert.All(set.Rects.Values, r => Assert.Equal(2, r.Page));

        for (int i = 0; i < 2; i++)
            Assert.Equal(new FrameRect(2, 1 + i * 30, 1, 28, 28), set.Rects[i]);
    }

    /// <summary>Verifies that Flip It! and Stop It! prize icon graphics share identical pixel data.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void FlipIt_and_StopIt_prize_icon_art_are_the_same_bitmap()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains("GM0_TURN.MPF"), "GM0_TURN.MPF is missing from this install");
        Assert.True(library.Contains("GM1_PCS.SPF"), "GM1_PCS.SPF is missing from this install");

        var turn = SheetImage.Read(library.Read("GM0_TURN.MPF"));
        var pcs = SheetImage.Read(library.Read("GM1_PCS.SPF"));
        var turnRects = SkinCatalog.RectsOf(AnimationTables.FlipItPrizeIcons, turn.Frames)!;
        var pcsRects = SkinCatalog.RectsOf(AnimationTables.StopItPrizeIcons, pcs.Frames)!;

        for (int i = 0; i < 2; i++)
        {
            var a = turnRects[i];
            var b = pcsRects[i];
            // Compare over Flip It!'s smaller rect: the row and column GM1_PCS carries beyond it are the
            // keyed-out index 1 that makes the two render alike despite the different corners.
            var fromTurn = SpriteSheetSlicer.RectCell(turn.Frames[a.Page], a.X, a.Y, a.X + a.W - 1, a.Y + a.H - 1);
            var fromPcs = SpriteSheetSlicer.RectCell(pcs.Frames[b.Page], b.X, b.Y, b.X + a.W - 1, b.Y + a.H - 1);
            Assert.True(fromTurn.Pixels.SequenceEqual(fromPcs.Pixels), $"cell {i}: expected identical art");
        }
    }

    [Fact]
    public void FindIt_pieces_tags_the_three_unreachable_prizes_unused_but_not_prize_icons()
    {
        var pieces = AnimationTables.FindItPieces;
        Assert.Equal("Drakko", pieces.Anims[0].DisplayName);
        Assert.Equal("Extra Dug", pieces.Anims[1].DisplayName);
        Assert.Contains("(unused)", pieces.Anims[2].DisplayName);
        Assert.Contains("(unused)", pieces.Anims[3].DisplayName);
        Assert.Contains("(unused)", pieces.Anims[4].DisplayName);

        var prizeIcons = AnimationTables.FindItPrizeIcons;
        Assert.All(prizeIcons.Anims, a => Assert.DoesNotContain("(unused)", a.DisplayName));
    }

    /// <summary>Guards that Stop It! and Find It! prize icons preserve their distinct per-board palette recolors.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void StopIt_and_FindIt_prize_icon_art_are_not_the_same_bitmap()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains("GM1_PCS.SPF"), "GM1_PCS.SPF is missing from this install");
        Assert.True(library.Contains("GM3_PCS.SPF"), "GM3_PCS.SPF is missing from this install");

        var sheet1 = SheetImage.Read(library.Read("GM1_PCS.SPF"));
        var sheet3 = SheetImage.Read(library.Read("GM3_PCS.SPF"));
        var rects1 = SkinCatalog.RectsOf(AnimationTables.StopItPrizeIcons, sheet1.Frames)!;
        var rects3 = SkinCatalog.RectsOf(AnimationTables.FindItPrizeIcons, sheet3.Frames)!;

        for (int i = 0; i < 2; i++)
        {
            var r1 = rects1[i];
            var r3 = rects3[i];
            var cell1 = SpriteSheetSlicer.RectCell(sheet1.Frames[r1.Page], r1.X, r1.Y, r1.X + r1.W - 1, r1.Y + r1.H - 1);
            var cell3 = SpriteSheetSlicer.RectCell(sheet3.Frames[r3.Page], r3.X, r3.Y, r3.X + r3.W - 1, r3.Y + r3.H - 1);
            Assert.False(cell1.Pixels.SequenceEqual(cell3.Pixels), $"cell {i}: expected the two boards' own recolors to differ");
        }
    }
}
