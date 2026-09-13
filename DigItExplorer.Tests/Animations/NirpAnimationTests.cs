using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Nirp animation tables and rendered frame hashes against reference files.</summary>
public class NirpAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("fly_egg", [0, 1, 2, 3, 4, 5, 6, 7, 8], AnimMode.Loop),
        ("turn_egg", [9, 10, 11, 12, 13], AnimMode.Once),
        ("fly", [14, 15, 16, 17, 18, 19, 20, 21, 22], AnimMode.Loop),
        ("turn", [23, 24, 25, 26, 27], AnimMode.Once),
        ("inflate", [28, 29, 30, 31, 32, 33, 34, 35, 36], AnimMode.Once),
        ("deflate", [36, 35, 34, 33, 32, 31, 30, 29, 28], AnimMode.Once),
        ("fall_away", [37], AnimMode.Pose),
    ];

    // Per world skin, one SHA-256 per animation. Nirp ships two skins; the third suffix is asserted
    // absent below rather than listed here.
    private static readonly (string Suffix, string Pal, string[] Shas)[] Reference =
    [
        ("00", "LVL000.PAL",
        [
            "2AAFF5A83B630BE03AAB85C9ED65EF0B6644DE8305F39638D082D1A939067D84",
            "5D3F221503150B6B4C18FAC27EAD4C410BDDEAB9743F0961E035D7BDFBBB187E",
            "569F87AF3212C80BD73448026DD438BC2AD94B48CF54DFA3172132F9ED44364B",
            "8809B7093C428D3BB3A46550857499311192955C97C63B082FC9CE348478BBD6",
            "9CAE4D2A0A88F1786B41A7761A62F5582C2E3B4D0731920288809EC5125CC223",
            "8B8A8BB59FB1A89AB2561A8BA121B5A932ED6D787CE0C8A5E5DFE07BC7F5FC8C",
            "5CD4C5A04163406842696FDFADE606D255D8DD324829D6F966D084B1B2ADD1D7",
        ]),
        ("02", "LVL400.PAL",
        [
            "BB4AB5400514CFEB670C2AA2C92B728F8F04DDD6BE251B6FCDF6F867A2B59659",
            "BD1BBFB3BCE84C932644D73FDA93043438C243DA15B2E799AC7B188BB07DA049",
            "A4ABBFED9A4BFAC835BD70A15CB35A66AAC34EF8672A80F5C5DEA4D143016A53",
            "3513D7BE4639C03788B8846C1CD75E10C294A98482AD0BC4D00C8DEE79FE0A59",
            "60559E62E85392831948938711EA54442DD3EB051763F8E9EC49423485E7C6CA",
            "086D7900E9E8BEB9FF184AC99C5630212619CAABE36593DBB586DF8ADBCFE180",
            "83CE11731AF2E39D5AEA2E8A6A0A6AE4E9EC8AA54B0C803BF5AD5DDF5D8FEBF9",
        ]),
    ];

    [Fact]
    public void Nirp_table_matches_reference_transcription()
    {
        var set = AnimationTables.Nirp;
        Assert.Equal("Nirp", set.Name);
        Assert.Equal((byte)0x0A, set.Category);
        Assert.Null(set.Rects); // grid-based, unlike Nirpling
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Nirp_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x0A, out var grid));
        var anims = AnimationTables.Nirp.Anims;

        // Asserted, not skipped: a skin the table claims does not exist has to be shown not to exist,
        // otherwise a missing file and a forgotten entry look the same from here.
        Assert.False(library.Contains($"{grid.Sheet}03.SPF"));
        Assert.False(library.Contains($"{grid.Sheet}03.MPF"));

        foreach (var (suffix, palName, shas) in Reference)
        {
            var pages = SheetImage.Read(library.Read($"{grid.Sheet}{suffix}.SPF")).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            AnimationGuard.AssertGridHashes(grid, pages, palette, anims, shas, suffix);
        }
    }
}
