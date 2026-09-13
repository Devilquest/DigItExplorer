using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Papa Spurk animation tables and rendered frame hashes against reference files.</summary>
public class PapaSpurkAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("walk", [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], AnimMode.Loop),
        ("turn", [10, 11, 12, 13, 14], AnimMode.Once),
        ("slam", [15, 16, 17, 18, 19, 20, 21, 22, 23, 15, 16, 17, 18, 19, 20, 21, 22, 23,
                  15, 16, 17, 18, 19, 20, 21, 22, 23, 15, 15, 15, 15, 15], AnimMode.Once),
        ("charge", [24, 25, 26, 27, 28, 29, 30, 31, 32, 33], AnimMode.Loop),
        ("fall_away", [35], AnimMode.Pose),
    ];

    // Per world skin, one SHA-256 per animation. Both skins on disk carry art for every animation, so
    // neither list has a null: the two differ, and hashing both is what shows they do.
    private static readonly (string Suffix, string Pal, string[] Shas)[] Reference =
    [
        ("00", "LVL000.PAL",
        [
            "2BFDBEEEDB6189EF180CC0133897541EDD544619316272D42856ADF57D3F61FB",
            "C3EDA38D23162A7DF65BA7D2DCCA9421617670593C410A9D0B61B6AFADDAA227",
            "377A43A49F2F0068D22000FD433C33EE894B0D328B254C2D901A3396534744AC",
            "E9CCD71EC9C2F622E717D6ECC48056FC2835A8CF17499F6F793F5DE9F5E0449E",
            "5BAC1B4F7AC1188694D516E45AF12B39147C741E33932D9BA807E95281DE70D4",
        ]),
        ("02", "LVL400.PAL",
        [
            "5E50B2C0E5FA59960312A7CF1FA712247EE5E96917D9D1DE6397468ADB0D2B1E",
            "238748E1AE69D35A33473B29568CF6DC391E5B34487E36B446145D545B4AEBD8",
            "05C72FB858EC140AB3F5F837FBDA09231526D3188786A2B87D034DA4276B7C77",
            "4D95EB0EEEC4B8DF30B818BBAAB060C3435CE8BDCAD974600AA42527FD548960",
            "774E65DA072196B963A7ED2A36CAC6BD046B172907BFF79F4825A66353AD183B",
        ]),
    ];

    [Fact]
    public void PapaSpurk_table_matches_reference_transcription()
    {
        var set = AnimationTables.PapaSpurk;
        Assert.Equal("Papa Spurk", set.Name);
        Assert.Equal((byte)0x0E, set.Category);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void PapaSpurk_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x0E, out var grid));
        var anims = AnimationTables.PapaSpurk.Anims;

        // WO_POP00 genuinely exists on disk (unlike Spurk/Hopper's absent world skins) but is a confirmed
        // shipped-but-never-loaded unused variant, not silently presented as real content.
        Assert.True(library.Contains("WO_POP00.MPF"));
        Assert.Contains("WO_POP00.MPF", KnownResources.Unused);

        // No underworld variant ships at all: a genuine absence.
        Assert.False(library.Contains("WO_POP03.SPF"));
        Assert.False(library.Contains("WO_POP03.MPF"));

        foreach (var (suffix, palName, shas) in Reference)
        {
            // WO_POP ships only as .MPF (a 2-page sheet), no flat .SPF at all for this character.
            var pages = SheetImage.Read(library.Read($"WO_POP{suffix}.MPF")).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            AnimationGuard.AssertGridHashes(grid, pages, palette, anims, shas, suffix);
        }
    }
}
