using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Rocker animation tables and rendered frame hashes against reference files.</summary>
public class RockerAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode, bool Seamless)[] ExpectedTable =
    [
        ("sniff", [0, 1, 2, 3, 4, 5, 4, 5, 4, 5, 4, 3, 2, 1, 0], AnimMode.Once, true),
        ("roll", [6, 7, 8, 9, 10, 11, 12, 13, 10, 11, 12, 13, 10, 11, 12, 13, 10, 11, 12, 13, 10, 11, 12, 13, 10, 9, 8, 7, 6],
         AnimMode.Once, true),
        ("fall", [10, 11, 12, 13], AnimMode.Loop, false),
        ("turn", [14, 15, 16, 17, 18], AnimMode.Once, false),
        ("inflate", [19, 20, 21, 22, 23, 24, 25, 26, 27], AnimMode.Once, false),
        ("deflate", [27, 26, 25, 24, 23, 22, 21, 20, 19], AnimMode.Once, false),
        ("fall_away", [28], AnimMode.Pose, false),
        ("walk", [29, 30, 31, 32, 33, 34, 35, 36, 37, 38], AnimMode.Loop, false),
    ];

    // Per world skin, one SHA-256 per animation. Rocker ships two skins; the third suffix is asserted
    // absent below rather than listed here.
    private static readonly (string Suffix, string Pal, string[] Shas)[] Reference =
    [
        ("00", "LVL000.PAL",
        [
            "78304A335413B350E7CE106922B55DBAC845468E46C3A157DEC3DDE665A91475",
            "07D511BAD03B85E97D18B3CA647CA0FC8AC90886D15A7F46430E2C8FF447973D",
            "06B11B9A22A3F701FD3E3B04FC72600115BF8307536956FF83FD982BD508A50A",
            "590CE91802A468A67533AB2451680CF347DE018AEC50ADDF340E1DFDE8B0241D",
            "E1C4B0BFBE431A40D7E77760E71FADAA1A4D0240CF84F4CBED9CEAD447B0987F",
            "1E387112B29DFC29C347C5158A20B26E63E3ED4B56D40F396898F096DB5AAAED",
            "E6A2A6920E82F89F1576C58199CD93105232D2088A0AC7B8F597AEFE374678D2",
            "49B2503FED265AEFF2972DCB7BBD2AE4796253BF299B4EDE00E9ACDC216D0A80",
        ]),
        ("02", "LVL400.PAL",
        [
            "F8A19D57970E444C5FD14235FD91D5787D287469A614DCFE460FFCE6F1367CCE",
            "413FFBB673C53DEBC7DA33919BFB5A5FEA51160A845FCFA37AE887536699EC02",
            "190665A6B70B8A61EF386E1F44422F9803BC1CDA690BB503679FA1C03781B5A4",
            "4021954AAAC16C6C37F6CC86732DF745F859E8E5CE5ED591A48794D4DE44228A",
            "5F61223F4E735F73134E17C8F8B6D70488B2070F2F800D31B403346FB8B7EA81",
            "637479DE7DECDAB2CB9A20D6B401165B5B6142D64142118C36247F3D30BFDD39",
            "2B08F8B5DFB0FBBE1D040D77B45EB8803522387D784920A4B57634C497ABF450",
            "1FEFD38A32787C8CE56F10C2326A8717C3A8A32674589ECC097BEE096746E770",
        ]),
    ];

    [Fact]
    public void Rocker_table_matches_reference_transcription()
    {
        var set = AnimationTables.Rocker;
        Assert.Equal("Rocker", set.Name);
        Assert.Equal((byte)0x08, set.Category);
        Assert.Equal(ExpectedTable.Length, set.Anims.Count);

        for (int i = 0; i < ExpectedTable.Length; i++)
        {
            Assert.Equal(ExpectedTable[i].Name, set.Anims[i].Name);
            Assert.Equal(ExpectedTable[i].Frames, set.Anims[i].Frames);
            Assert.Equal(ExpectedTable[i].Mode, set.Anims[i].Mode);
            Assert.Equal(ExpectedTable[i].Seamless, set.Anims[i].Seamless);
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Rocker_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x08, out var grid));
        var anims = AnimationTables.Rocker.Anims;

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
