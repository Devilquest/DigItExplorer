using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Flip It! and Spin It! bonus minigame animation tables and frame hashes against reference files.</summary>
public class MinigameAnimationTests
{
    // Ascending frame order with each reversed animation immediately after the forward one it mirrors,
    // which is the table convention; the hashes below follow the same order.
    private static readonly (string Name, int First, int Last, bool Reversed)[] ExpectedFlipIt =
    [
        ("turn_up", 0, 4, false), ("turn_down", 0, 4, true),
        ("reveal_dragon", 5, 9, false), ("hide_dragon", 5, 9, true),
        ("reveal_gold", 10, 14, false), ("hide_gold", 10, 14, true),
        ("reveal_gem25", 15, 19, false), ("hide_gem25", 15, 19, true),
        ("reveal_gem50", 20, 24, false), ("hide_gem50", 20, 24, true),
        ("reveal_energy", 25, 29, false), ("hide_energy", 25, 29, true),
    ];

    private static readonly string[] FlipItShas =
    [
        "DD579E84DB774F22E00A6C4BA1C8EFAF3E4AD0FA85A2E1D7954B9C6B9C9F1B41", // turn_up
        "2D343B136C7E8D2ED24E6E7F74C60D9FB2A7D56216E309E47F014A9768046DB0", // turn_down
        "AD8B922EE50BAF5A7044FBAEB01658C475DDAEA3BD231389215822CADB40D4DD", // reveal_dragon
        "65A2E00ACF367B5B59065632C1749D71BB264EA584B1A93C80124FAE6748D043", // hide_dragon
        "3BFF25C49D789E17C90999D112846E715517A749921D76880908CBEFA478A3C2", // reveal_gold
        "7573B7C30A38A0448CCE0B64A676D516096F8727E89EB9D00A7CD5BD58DC62C8", // hide_gold
        "1BA021324962C1B0C32E487B30CC4422ABDEFB9BE54695F1E7731B50446B9510", // reveal_gem25
        "5CE1BB1BD35AB9FBE5ED9FD84B5AAB6E5AC3B90D6F6028DD0374F5E087B7CA26", // hide_gem25
        "3B36606B175B922F4FF81AFB05809C17B56F62B50D0D73BCD9CD0B032BD06BB3", // reveal_gem50
        "E3D77214936599D43BD74A231C54784FA7D41417174C2EEA16B48EB9685A416A", // hide_gem50
        "BF09D0578EC4CE8B529D2609E0773D3CAE6166B9181FA94228183AD99DC72035", // reveal_energy
        "4016A889473735626A1BDAFA70ED1D31EDAEC2ABA4399629BB85BE748577C11F", // hide_energy
    ];

    private const string SpinItSha = "26EFD975636BD840C41DBA5AD0AAB2EE20D326CC74A1FF99396795E96FADDA1B";

    [Fact]
    public void FlipIt_table_matches_reference_transcription()
    {
        var set = AnimationTables.FlipIt;
        Assert.Equal("Flip It!", set.Name);
        Assert.Equal("GM0_TURN", set.FixedSheet);
        Assert.Equal("GM0_PAL.PAL", set.FixedPalette);
        Assert.Null(set.TransparentIndices);
        Assert.Equal(ExpectedFlipIt.Length, set.Anims.Count);

        for (int i = 0; i < ExpectedFlipIt.Length; i++)
        {
            var (name, first, last, reversed) = ExpectedFlipIt[i];
            var expectedFrames = Enumerable.Range(first, last - first + 1);
            if (reversed) expectedFrames = expectedFrames.Reverse();
            Assert.Equal(name, set.Anims[i].Name);
            Assert.Equal(expectedFrames, set.Anims[i].Frames);
            Assert.Equal(AnimMode.Once, set.Anims[i].Mode);
        }

        // The cards are separator-packed, so the geometry is read from the sheet (see the rect test below).
        Assert.True(set.SlicedSheet);
        Assert.Null(set.Rects);
        Assert.DoesNotContain(set.Anims, a => a.Name.StartsWith("mask"));
    }

    /// <summary>Guards that a Flip It! card and a Stop It! reel name the same prize the same way.</summary>
    [Fact]
    public void FlipIt_cards_name_their_prizes_as_the_reel_symbols_do()
    {
        var prizes = AnimationTables.StopItPieces.Anims.Select(a => a.DisplayName!).ToHashSet();
        var cards = AnimationTables.FlipIt.Anims
            .Where(a => a.Name.StartsWith("reveal_") || a.Name.StartsWith("hide_"))
            .ToList();

        Assert.Equal(2 * prizes.Count, cards.Count);
        foreach (var card in cards)
        {
            var verb = card.Name.StartsWith("reveal_") ? "Reveal " : "Hide ";
            var label = SkinCatalog.PrettyAnimName(AnimationTables.FlipIt.Name, card);
            Assert.StartsWith(verb, label, StringComparison.Ordinal);
            Assert.Contains(label[verb.Length..], prizes);
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void FlipIt_rects_derived_from_the_sheet_are_the_two_card_pages()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.FlipIt;
        var pages = SheetImage.Read(library.Read("GM0_TURN.MPF")).Frames;
        var rects = SkinCatalog.RectsOf(set, pages);

        // 5×3 cards of 60×62 with a 1 px margin, over pages 0 and 1: numbered across pages, so page 1's
        // first card is id 15. Page 2 holds only the covering mask, which this table deliberately leaves
        // out, so nothing past id 29 is ever resolved.
        Assert.Equal(30, rects!.Count);
        Assert.Equal(new FrameRect(0, 1, 1, 60, 62), rects[0]);
        Assert.Equal(new FrameRect(0, 4 * 61 + 1, 2 * 63 + 1, 60, 62), rects[14]);
        Assert.Equal(new FrameRect(1, 1, 1, 60, 62), rects[15]);
        Assert.Equal(new FrameRect(1, 4 * 61 + 1, 2 * 63 + 1, 60, 62), rects[29]);
        Assert.DoesNotContain(rects.Keys, k => k > 29);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void FlipIt_cells_contain_neither_pages_separator_index()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        // Why this set keys nothing beyond index 0, and why adding a key "to drop the separator grid" would be
        // dropping a color that is not there: the separators bound the cells from outside, so a rect the slice
        // returns contains none of either page's index. Page 0 draws them in 195 and page 1 in 194, so a key
        // transcribed from one page would not even have covered the other.
        var set = AnimationTables.FlipIt;
        var pages = SheetImage.Read(library.Read("GM0_TURN.MPF")).Frames;
        var rects = SkinCatalog.RectsOf(set, pages)!;

        foreach (var frame in set.Anims.SelectMany(a => a.Frames).Distinct())
        {
            var rect = rects[frame];
            var cell = SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y,
                rect.X + rect.W - 1, rect.Y + rect.H - 1);
            Assert.DoesNotContain(cell.Pixels.ToArray(), p => p is 194 or 195);
        }
    }

    [Fact]
    public void SpinIt_table_matches_reference_transcription()
    {
        var set = AnimationTables.SpinIt;
        Assert.Equal("Spin It!", set.Name);
        Assert.Equal("GM2_SPIN", set.FixedSheet);
        Assert.True(set.UseEmbeddedPalette);
        Assert.Null(set.FixedPalette);
        Assert.Equal(new HashSet<byte> { 51, 52, 53, 54, 55, 56, 171 }, set.TransparentIndices);

        var spin = Assert.Single(set.Anims);
        Assert.Equal("spin", spin.Name);
        Assert.Equal(AnimMode.Loop, spin.Mode);
        Assert.Equal(Enumerable.Range(0, 36), spin.Frames);

        // 36 frames of 64×64 over three pages, 5 per row.
        Assert.Equal(36, set.Rects!.Count);
        Assert.Equal(new FrameRect(0, 0, 0, 64, 64), set.Rects[0]);
        Assert.Equal(new FrameRect(0, 4 * 64, 2 * 65, 64, 64), set.Rects[14]);
        Assert.Equal(new FrameRect(2, 0, 65, 64, 64), set.Rects[35]);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void SpinIt_ships_no_companion_palette_file()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        // The reason UseEmbeddedPalette exists at all: unlike every other sheet in the tables, this one has
        // no .PAL of its own to fall back to, under either the sheet's name or the GM0_PAL naming pattern.
        Assert.True(library.Contains("GM0_PAL.PAL"));
        Assert.False(library.Contains("GM2_SPIN.PAL"));
        Assert.False(library.Contains("GM2_PAL.PAL"));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void FlipIt_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.FlipIt;
        var pages = SheetImage.Read(library.Read("GM0_TURN.MPF")).Frames;
        var palette = VgaPalette.From6Bit(
            library.Read(set.FixedPalette!).AsSpan(0, 768));
        AssertAnimShas(set, pages, palette, FlipItShas);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void SpinIt_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.SpinIt;
        var sheet = SheetImage.Read(library.Read("GM2_SPIN.MPF"));
        AssertAnimShas(set, sheet.Frames, sheet.Palette, [SpinItSha]);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Minigame_cells_are_blank_without_their_extra_transparent_keys()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        // The wheel's own cells sit on a solid teal backdrop that fills the whole 64×64 cell, so an
        // index-0-only emptiness test can never report a blank cell here even where one exists, which is
        // exactly why the presence check has to honor the sheet's extra transparent keys.
        var set = AnimationTables.SpinIt;
        var sheet = SheetImage.Read(library.Read("GM2_SPIN.MPF"));
        var rect = set.Rects![0];
        var cell = SpriteSheetSlicer.RectCell(sheet.Frames[rect.Page], rect.X, rect.Y,
            rect.X + rect.W - 1, rect.Y + rect.H - 1);

        Assert.False(cell.IsEmpty);
        Assert.False(cell.IsEmptyFor(set.TransparentIndices)); // real arrow art survives the keys
        Assert.True(cell.IsEmptyFor(new HashSet<byte>(cell.Pixels.Distinct())));
    }

    private static void AssertAnimShas(CharacterAnimSet set, IReadOnlyList<byte[]> pages,
        VgaPalette palette, string[] shas)
    {
        var rects = SkinCatalog.RectsOf(set, pages)!;
        for (int i = 0; i < set.Anims.Count; i++)
        {
            using var sha = SHA256.Create();
            foreach (var f in set.Anims[i].Frames)
            {
                var rect = rects[f];
                var cell = SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y,
                    rect.X + rect.W - 1, rect.Y + rect.H - 1);
                var rgba = new byte[cell.W * cell.H * 4];
                AnimationPixels.ToRgba(cell, palette, rgba, set.TransparentIndices);
                sha.TransformBlock(rgba, 0, rgba.Length, null, 0);
            }
            sha.TransformFinalBlock([], 0, 0);
            Assert.Equal(shas[i], Convert.ToHexString(sha.Hash!), ignoreCase: true);
        }
    }
}
