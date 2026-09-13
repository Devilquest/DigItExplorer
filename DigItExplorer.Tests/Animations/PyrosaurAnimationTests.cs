using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Pyrosaur animation tables and rendered frame hashes against reference files.</summary>
public class PyrosaurAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("walk", [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15], AnimMode.Loop),
        ("turn", [16, 17, 18, 19, 20], AnimMode.Once),
        ("inflate", [21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31], AnimMode.Once),
        ("deflate", [31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21], AnimMode.Once),
        ("telegraph", [34, 35, 34, 35, 34, 35, 34, 35, 34, 35], AnimMode.Once),
        ("fire_breath", [36, 37, 38, 39, 40, 41, 42], AnimMode.Once),
        ("fall_away", [45], AnimMode.Pose),
    ];

    // Per world skin, one SHA-256 per animation. The "00" and "03" lists are identical on purpose, and it
    // takes both halves to explain why: the two files are the same bytes, and the indices this sprite uses
    // hold the same colors in LVL000.PAL and LVL600.PAL even though the palettes differ in 79 of 256
    // entries. Equal art alone would not have been enough.
    private static readonly (string Suffix, string Pal, string[] Shas)[] Reference =
    [
        ("00", "LVL000.PAL",
        [
            "E4C95DED0BFBAF5505120B005FEC31C2A7C0FD4FC7EA9EC907680FFC8FC6B6A4",
            "5C963E80009130F194B8B82ADC8DC9238E45ACC2B60A733F4748B45C89A7897B",
            "0CF3D05AAE515BD5AB834A5DC9929E5AC2820170360C2F44307E2B23A1C327CB",
            "B80AB7748C629794A8112457FE7CBCF428131ADEFB50DD1A5C6C88C009E574E2",
            "C9BCC11C73E31C382CBF7D37293FA69ED2436C4425438A9FE75A6C94D2D0AD1A",
            "13BA6616A22F4E321A4EC5CBB387330BDFEBD749BC629130A70F1836ACAC3946",
            "E55B4914DEBC1F05464DBA27CC5AD0D28E7DC94507E81867CBAE2C2489415AA6",
        ]),
        ("02", "LVL400.PAL",
        [
            "CAAA33F360AB3A4F6875A9D3ADC1E71C1408B58FAB7A557B3F561B38099E01F0",
            "81114E09D225516F2AD65F4886D695DD5A1BE188BBCE5E633A7FEC4C24527393",
            "C62DFA1799A45CCC67D7C8C8A2F5C016B75828C0114F471381E55C373D7CA21C",
            "9F7CDE3472E35D3DCDE69E8E8D00B6F7A461534DC40CE211D3EA92FF5021256D",
            "32156F382364793FE54603B303BC2A532DFE4A62A8AB5DADDEC176F6FFE0C307",
            "DD0E94AE7096A98F9D54C7D6EFF019656BDDEEDC99BB575D3C672628AE8FB542",
            "56C71735EDF5869F164065272336078B5B585205E02228244BCD136A2E84FB34",
        ]),
        ("03", "LVL600.PAL",
        [
            "E4C95DED0BFBAF5505120B005FEC31C2A7C0FD4FC7EA9EC907680FFC8FC6B6A4",
            "5C963E80009130F194B8B82ADC8DC9238E45ACC2B60A733F4748B45C89A7897B",
            "0CF3D05AAE515BD5AB834A5DC9929E5AC2820170360C2F44307E2B23A1C327CB",
            "B80AB7748C629794A8112457FE7CBCF428131ADEFB50DD1A5C6C88C009E574E2",
            "C9BCC11C73E31C382CBF7D37293FA69ED2436C4425438A9FE75A6C94D2D0AD1A",
            "13BA6616A22F4E321A4EC5CBB387330BDFEBD749BC629130A70F1836ACAC3946",
            "E55B4914DEBC1F05464DBA27CC5AD0D28E7DC94507E81867CBAE2C2489415AA6",
        ]),
    ];

    [Fact]
    public void Pyrosaur_table_matches_reference_transcription()
    {
        var set = AnimationTables.Pyrosaur;
        Assert.Equal("Pyrosaur", set.Name);
        Assert.Equal((byte)0x09, set.Category);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void WO_RED00_is_a_byte_identical_duplicate_of_WO_RED03()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var red00 = library.Read("WO_RED00.MPF");
        var red02 = library.Read("WO_RED02.MPF");
        var red03 = library.Read("WO_RED03.MPF");

        Assert.Equal(SHA256.HashData(red03), SHA256.HashData(red00));
        Assert.NotEqual(SHA256.HashData(red03), SHA256.HashData(red02));

        Assert.Contains("WO_RED00.MPF", KnownResources.Unused);
        Assert.Contains("WO_RED02.MPF", KnownResources.Unused);
        Assert.DoesNotContain("WO_RED03.MPF", KnownResources.Unused);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Pyrosaur_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x09, out var grid));
        var anims = AnimationTables.Pyrosaur.Anims;
        int nFrames = anims.SelectMany(a => a.Frames).Max() + 1;

        foreach (var (suffix, palName, shas) in Reference)
        {
            var pages = SheetImage.Read(library.Read($"{grid.Sheet}{suffix}.MPF")).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            AnimationGuard.AssertGridHashes(grid, pages, palette, anims, shas, "Pyrosaur");
        }
    }
}
