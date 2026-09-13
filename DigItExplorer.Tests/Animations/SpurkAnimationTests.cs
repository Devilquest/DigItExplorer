using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Spurk animation tables and rendered frame hashes against reference files.</summary>
public class SpurkAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("retract", [0, 1, 2, 3, 4, 5, 6], AnimMode.Once),
        ("hidden", [6], AnimMode.Pose),
        ("emerge", [6, 7, 8, 9, 10, 11, 12, 13], AnimMode.Once),
        ("walk", [14, 15, 16, 17, 18, 19, 20, 21, 22, 23], AnimMode.Loop),
        ("turn", [24, 25, 26, 27, 28], AnimMode.Once),
        ("fall_away", [29], AnimMode.Pose),
    ];

    // Per world skin, one SHA-256 per animation. Spurk ships one skin; the other two suffixes are
    // asserted absent below rather than listed here.
    private static readonly (string Suffix, string Pal, string[] Shas)[] Reference =
    [
        ("00", "LVL000.PAL",
        [
            "8E80A227A2B9E62B6F65A66BABC9F1B57CC3A35B95FE580A6ACB01D32B265CE7",
            "0AE0FFF135CA9F3C3390981EED9423527FC9D2A1F1AE5C69828C8BC3A5568EED",
            "BF23D4F6301FE8236AE10D8D120F0BA33AE0CB472F73219E5E02D3C0BE118702",
            "631E54A9C18326C6C53D0CA9C339CF06F65C498E78B936278D9A42FA2A6E5DFE",
            "423F5858CBFA6ECCCF24D50D49F38D430B61037CD14B021CB8627047B81C53E5",
            "901440051675CE9D78970A77C3F2319A3E3689AE77F31EE3A1A52D6F464C268F",
        ]),
    ];

    [Fact]
    public void Spurk_table_matches_reference_transcription()
    {
        var set = AnimationTables.Spurk;
        Assert.Equal("Spurk", set.Name);
        Assert.Equal((byte)0x0C, set.Category);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Spurk_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x0C, out var grid));
        var anims = AnimationTables.Spurk.Anims;

        // Asserted, not skipped: a skin the table claims does not exist has to be shown not to exist,
        // otherwise a missing file and a forgotten entry look the same from here.
        foreach (var suffix in new[] { "02", "03" })
        {
            Assert.False(library.Contains($"{grid.Sheet}{suffix}.SPF"));
            Assert.False(library.Contains($"{grid.Sheet}{suffix}.MPF"));
        }

        foreach (var (suffix, palName, shas) in Reference)
        {
            var pages = SheetImage.Read(library.Read($"{grid.Sheet}{suffix}.SPF")).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            AnimationGuard.AssertGridHashes(grid, pages, palette, anims, shas, suffix);
        }
    }
}
