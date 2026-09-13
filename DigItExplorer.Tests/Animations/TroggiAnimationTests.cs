using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Troggi animation tables and rendered frame hashes against reference files.</summary>
public class TroggiAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("throw", [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13], AnimMode.Loop),
        ("inflate", [14, 15, 16, 17, 18, 19, 20, 21, 22], AnimMode.Once),
        ("deflate", [22, 21, 20, 19, 18, 17, 16, 15, 14], AnimMode.Once),
        ("fall_away", [23], AnimMode.Pose),
        ("turn", [24, 25, 26, 27, 28], AnimMode.Once),
    ];

    // Per world skin, one SHA-256 per animation. Troggi ships one skin; the other two suffixes are
    // asserted absent below rather than listed here.
    private static readonly (string Suffix, string Pal, string[] Shas)[] Reference =
    [
        ("02", "LVL400.PAL",
        [
            "3742803294CF8CBF0CDB9754AB41E43C3BF2767DF73543F960A6D970B541B58F",
            "BE44FAFA68D68BBC87B91AE373C7BD527C5CA127C6CFD777F009AC2EF3A2AB71",
            "731A958CCDE6D38EB2A52A8B08001BF558D27B50D6CC9BCD5E0CF51F7AFA0353",
            "08712A9735318BED47BC1325D4B35EE08499ACAC7C830441EDACE86BF95D262E",
            "D78839786EA0B4B72D958421196EEE7CCF310C0F917B330B57E0C21873F6FD28",
        ]),
    ];

    [Fact]
    public void Troggi_table_matches_reference_transcription()
    {
        var set = AnimationTables.Troggi;
        Assert.Equal("Troggi", set.Name);
        Assert.Equal((byte)0x0F, set.Category);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Troggi_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x0F, out var grid));
        var anims = AnimationTables.Troggi.Anims;

        // Asserted, not skipped: a skin the table claims does not exist has to be shown not to exist,
        // otherwise a missing file and a forgotten entry look the same from here.
        foreach (var suffix in new[] { "00", "03" })
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
