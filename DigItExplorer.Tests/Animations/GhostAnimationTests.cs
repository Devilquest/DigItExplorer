using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Ghost animation tables and rendered frame hashes against reference files.</summary>
public class GhostAnimationTests
{
    // idle and reappear are descending on purpose, unlike every other character here: idle's counter
    // decrements (seg3:0x3F5D) and the frame is the counter unmodified (0x3FBE), and reappear computes
    // 21 - counter (0x401B). Sorting either list ascending would play both backward.
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("idle", [9, 8, 7, 6, 5, 4, 3, 2, 1, 0], AnimMode.Loop),
        ("turn", [10, 11, 12, 13, 14], AnimMode.Once),
        ("vanish", [15, 16, 17, 18, 19, 20, 21], AnimMode.Once),
        ("hidden", [21], AnimMode.Pose),
        ("reappear", [21, 20, 19, 18, 17, 16, 15], AnimMode.Once),
    ];

    // Per variant: one SHA-256 per animation over its concatenated RGBA frames in table order.
    private static readonly (string Character, string[] Shas)[] Reference =
    [
        ("Ghost Slugger",
        [
            "83D8B93A71F19DA6844B4B26EA6729C50B7AC801012377065C627B76AF27BD4F",
            "DBCC3820884381AD28CBFA6A8966040E6F3DB9192E850C01059F74AFB8B637DE",
            "12C5AE267FFB01286C207420EF06EDE5241987706339F3420A98E453B1ABBDA7",
            "3B79E49D92AFA6B83A6CE6EA65F2D233F2354A6C345F5EA66B9A71EC778344F8",
            "226F848B8D39424D940B588A3B4D4FC1CB19D89D3326E7B3179CE81366CA0D7E",
        ]),
        ("Ghost Draggo",
        [
            "667F5D385B742AF748FDE8E51B6EA6292DA887C229A21E1078A4C66BD945AE56",
            "F1930C2B0311926207A7AC4AB9335BA1405EC2C75CB82FF75026D81997B8AC23",
            "D2A9392770626E663A67E941C1D3B64208C8602EB9F28F452338F5EDF50FA7DF",
            "89805A2980A31D8F7C03E92756FF6D8A85F69500AD6197E4D650378D92E6173B",
            "AE96DDAF226D9849724F11FB756C1292D73F791F7A9D540C6ECCF65FCC5C9EF3",
        ]),
        ("Ghost Rocker",
        [
            "EBF373D89529566311E5472FC121AB923C020D00019C817CF5C56B3EF41AC2D1",
            "BABA1FB916B16EC4409EA8B7CFA3DC2DC7B0B236D357150A600D7930E882BB14",
            "41665462FFB2DE1B45EB14C8F6E61740E5B0D8C39C46FA7456AB6461E628275B",
            "CEB94582F746962C4828807E27024A56987746732879D9582FB961FCF2F8E874",
            "90A60252F4B80F58D98B4375C5010183F2F72EB7D40E03AF6488D8CBEE456020",
        ]),
        ("Ghost Pyrosaur",
        [
            "590647D70F26B3C881241B9FE5FEFA0F77A7224033E7B4C5E9EDB67669D8A878",
            "B9047CE7D5F68A4559B98C07FD07C4CC88107F5CD02349E58F588613A59402D9",
            "AE7B12D08F4A7AAC0551E9D8B99402C8D5FD8CE2448AC98829CFF19D0DBD6FC7",
            "BDCD85066D886BC9B16ED900AE1B2FF3B1062817DD9CE98FCEF6A6EFC60E1EB5",
            "049788AADE8803CAA5877F4C0FF22CEAF8768866FBDD7EA96E2B0C90BA0FF34B",
        ]),
    ];

    private static readonly (string Name, CharacterAnimSet Set)[] Sets =
    [
        ("Ghost Slugger", AnimationTables.GhostSlugger),
        ("Ghost Draggo", AnimationTables.GhostDraggo),
        ("Ghost Rocker", AnimationTables.GhostRocker),
        ("Ghost Pyrosaur", AnimationTables.GhostPyrosaur),
    ];

    [Theory]
    [MemberData(nameof(SetNames))]
    public void Ghost_table_matches_reference_transcription(string name)
    {
        var set = Sets.First(s => s.Name == name).Set;
        Assert.Equal((byte)0x13, set.Category);
        Assert.Equal("WO_GHOST", set.FixedSheet);
        Assert.Equal("LVL040.PAL", set.FixedPalette);
        // Spookstone is the shared word across all three ghost level names in the executable.
        Assert.Equal("Spookstone", set.FixedLocation);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Fact]
    public void All_four_variants_share_the_same_animation_table_instance()
    {
        Assert.Same(AnimationTables.GhostSlugger.Anims, AnimationTables.GhostDraggo.Anims);
        Assert.Same(AnimationTables.GhostSlugger.Anims, AnimationTables.GhostRocker.Anims);
        Assert.Same(AnimationTables.GhostSlugger.Anims, AnimationTables.GhostPyrosaur.Anims);
    }

    [Fact]
    public void Rects_are_the_literal_per_variant_dict_comprehensions()
    {
        // First and last cell of each variant, so a wrong stride or page break fails here rather than
        // surviving a count of 22.
        Assert.Equal(new FrameRect(0, 0, 0, 32, 19), AnimationTables.GhostSlugger.Rects![0]);
        Assert.Equal(new FrameRect(0, 3 * 33, 2 * 20, 32, 19), AnimationTables.GhostSlugger.Rects[21]); // i=21 -> col 3, row 2

        Assert.Equal(new FrameRect(0, 0, 60, 38, 30), AnimationTables.GhostDraggo.Rects![0]);
        Assert.Equal(new FrameRect(0, 5 * 39, 2 * 31 + 60, 38, 30), AnimationTables.GhostDraggo.Rects[21]); // i=21 -> col 5, row 2

        Assert.Equal(new FrameRect(0, 0, 153, 29, 25), AnimationTables.GhostRocker.Rects![0]);
        Assert.Equal(new FrameRect(1, 0, 0, 29, 25), AnimationTables.GhostRocker.Rects[10]);
        Assert.Equal(new FrameRect(1, 0, 26, 29, 25), AnimationTables.GhostRocker.Rects[20]);
        Assert.Equal(new FrameRect(1, 30, 26, 29, 25), AnimationTables.GhostRocker.Rects[21]);

        Assert.Equal(new FrameRect(1, 0, 52, 42, 36), AnimationTables.GhostPyrosaur.Rects![0]);
        Assert.Equal(new FrameRect(1, 0 * 43, 3 * 37 + 52, 42, 36), AnimationTables.GhostPyrosaur.Rects[21]); // i=21 -> col 0, row 3

        foreach (var (_, set) in Sets)
            for (int i = 0; i < 22; i++)
                Assert.True(set.Rects!.ContainsKey(i));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory]
    [MemberData(nameof(SetNames))]
    public void Ghost_frame_pixels_match_reference_pixels(string name)
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = Sets.First(s => s.Name == name).Set;
        var shas = Reference.First(r => r.Character == name).Shas;

        var pages = SheetImage.Read(library.Read("WO_GHOST.MPF")).Frames;
        var palette = VgaPalette.From6Bit(library.Read("LVL040.PAL").AsSpan(0, 768));

        AnimationGuard.AssertRectHashes(set, pages, palette, shas);
    }

    /// <summary>Variant names, for xUnit's per-case <c>[Theory]</c> display.</summary>
    public static IEnumerable<object[]> SetNames() => Sets.Select(s => new object[] { s.Name });
}
