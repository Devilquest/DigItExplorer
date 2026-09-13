using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Hopper animation tables and rendered frame hashes against reference files.</summary>
public class HopperAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("hop", [7, 8, 9, 0, 0, 1, 1, 2, 3], AnimMode.Loop),
        ("leap", [4, 4, 4, 5, 5, 6], AnimMode.Once),
        ("fall", [7], AnimMode.Pose),
        ("turn", [10, 11, 12, 13, 14], AnimMode.Once),
        ("inflate", [15, 16, 17, 18, 19, 20, 21, 22, 23], AnimMode.Once),
        ("deflate", [23, 22, 21, 20, 19, 18, 17, 16, 15], AnimMode.Once),
        ("fall_away", [24], AnimMode.Pose),
        ("walk", [25, 26, 27, 28, 29, 30, 31, 32, 33, 34], AnimMode.Loop),
    ];

    // Per world skin, one SHA-256 per animation. Hopper ships two skins; the third suffix is asserted
    // absent below rather than listed here.
    private static readonly (string Suffix, string Pal, string[] Shas)[] Reference =
    [
        ("00", "LVL000.PAL",
        [
            "F6E98192D8B568B23DE7F6D8227FFEBA261CBEC1EAE96963A709769E15A32FC0",
            "53452CD6D4A59244F4DBCCB21C80DA0E506EF3B80C574EBB5C1BC15105512203",
            "753883BD545297728C30AFF1D8DEB19D78C7855324052BC3C259007339D53F15",
            "BBA827C53AD338BD45A1A24F10FEA589AC8F7EF4D45680F7280CE0CA6D2AF235",
            "3718FC363D1698368D0462130C9E13DF48E37608B4788EC96B04F942986CED74",
            "40DB9067EB440ACB4D2485CBBAFBA122A4F1C5464335D76E8644DE00A372DD62",
            "7D52ED50494CA969C9A7A846FFC2E38810F33821FDD40E814497CE2E2B149CE3",
            "7F97F2B151CA83B2096A58B1941804A8AAEBAA84ADB9C3076F91816E6BD8D67A",
        ]),
        ("02", "LVL400.PAL",
        [
            "40384DAD5BB545DDCFE139BA7D91670B687BF3EC1E74FCE7A6730323E957D69B",
            "0857265BAA043ED5A8447630E19182B31EEA09A04AF8B161F3BCF51FECE4A667",
            "52ACCA6CAC600459EEFFAE62E60EEDC6A0422435CB22901B125991336BF7EDF1",
            "D1AB4739D57A2F8551AA5B677D89832EBDC8D617B3B2C809B9B9E82BE447D290",
            "D29FE59769DFEFE6D40929C354218BC61469D96CE546B4F3F9BE169B455866D8",
            "F5008E2E3EFECA3F42DBE81FA01AD8B9407E1B0C7546CAB18E937BE693788759",
            "F523B7415DCC23C4F49BE00346BC5A915CFC831C504C96D9C63F432AFDAA90B7",
            "422F5AB77B3704E36605CA5F54AB75ECCB0ED773E8FE144135B6FE76AE44C862",
        ]),
    ];

    [Fact]
    public void Hopper_table_matches_reference_transcription()
    {
        var set = AnimationTables.Hopper;
        Assert.Equal("Hopper", set.Name);
        Assert.Equal((byte)0x0D, set.Category);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Hopper_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x0D, out var grid));
        var anims = AnimationTables.Hopper.Anims;

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
