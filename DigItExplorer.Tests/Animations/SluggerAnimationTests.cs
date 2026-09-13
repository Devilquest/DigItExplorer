using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Slugger animation tables and rendered frame hashes against reference files.</summary>
public class SluggerAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("shell_spin", [0, 1, 2, 3], AnimMode.Loop),
        ("shell_rest", [4], AnimMode.Pose),
        ("emerge", [4, 5, 6, 7, 8, 9, 10, 11], AnimMode.Once),
        ("walk", [12, 13, 14, 15, 16, 17, 18, 19, 20, 21], AnimMode.Loop),
        ("turn", [22, 23, 24, 25, 26], AnimMode.Once),
        ("fall_away", [27], AnimMode.Pose),
    ];

    // Per world skin, one SHA-256 per animation. The two lists are identical on purpose: these two skins
    // ship byte-identical art, so equal hashes are the expected result and not a copy-paste slip.
    private static readonly (string Suffix, string Pal, string[] Shas)[] Reference =
    [
        ("00", "LVL000.PAL",
        [
            "D09BCBCED57AC59E93ED2A8C5393AD795A9613387D9263D63F7EFA80B76EDD59",
            "04A895622371586BAF25B518D565410CFC8FBE906572A6D90188F74E4D91F182",
            "B75EF28F16F2B2D9644FADF1B1BF3BE969E943169FA793CBC6C171D1000D56CE",
            "C44D298E15AB15368C59BD4D2E47CFF3C016B3FF7F1CE3C104393D18CCED9D35",
            "F53765A793DFB73ECC515CF84EC1B2838F9989A1248E8EE794661CDB35CCA66F",
            "A0D47E8D11C8F4231185235510359E5451248D8D4E55A89606BFC60A39815347",
        ]),
        ("03", "LVL600.PAL",
        [
            "D09BCBCED57AC59E93ED2A8C5393AD795A9613387D9263D63F7EFA80B76EDD59",
            "04A895622371586BAF25B518D565410CFC8FBE906572A6D90188F74E4D91F182",
            "B75EF28F16F2B2D9644FADF1B1BF3BE969E943169FA793CBC6C171D1000D56CE",
            "C44D298E15AB15368C59BD4D2E47CFF3C016B3FF7F1CE3C104393D18CCED9D35",
            "F53765A793DFB73ECC515CF84EC1B2838F9989A1248E8EE794661CDB35CCA66F",
            "A0D47E8D11C8F4231185235510359E5451248D8D4E55A89606BFC60A39815347",
        ]),
    ];

    [Fact]
    public void Slugger_table_matches_reference_transcription()
    {
        var set = AnimationTables.Slugger;
        Assert.Equal("Slugger", set.Name);
        Assert.Equal((byte)0x06, set.Category);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Slugger_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x06, out var grid));
        var anims = AnimationTables.Slugger.Anims;

        foreach (var (suffix, palName, shas) in Reference)
        {
            var sheetName = $"{grid.Sheet}{suffix}.SPF";
            Assert.True(library.Contains(sheetName), $"{sheetName} is missing from this install");

            var pages = SheetImage.Read(library.Read(sheetName)).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            AnimationGuard.AssertGridHashes(grid, pages, palette, anims, shas, suffix);
        }
    }
}
