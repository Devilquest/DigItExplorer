using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Grock animation tables and rendered frame hashes against reference files.</summary>
public class GrockAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("fly", [0, 1, 2, 3, 4, 5, 6, 7, 8], AnimMode.Loop),
        ("throw", [9, 10, 11, 12, 13, 14, 15, 16, 17], AnimMode.Loop),
        ("turn", [18, 19, 20, 21, 22], AnimMode.Once),
        ("inflate", [23, 24, 25, 26, 27, 28, 29, 30, 31], AnimMode.Once),
        ("deflate", [31, 30, 29, 28, 27, 26, 25, 24, 23], AnimMode.Once),
        ("fall_away", [32], AnimMode.Pose),
    ];

    // One SHA-256 per animation over its concatenated RGBA frames, in table order.
    private static readonly string[] Shas =
    [
        "46A36003AA8200446F8BAB328A0426BE1E431F280012FF792DABFE71AF2C0404", // fly
        "6D404A78279A1ADB9BA490589C0442B73F9988FEA5DD912641AD44F76990D5CF", // throw
        "261709A5CBA06938BB7828D2E3B79E674DC6D8798C194B3ABC2E8E2B215588E7", // turn
        "DC194AC285B4E470993240EE462A617C82E1CCBCEAB70A76268DC7E3E0A52926", // inflate
        "7290C2483694AE940BFF0C4FA47DB7E4A06CE9524DE8624BF699591DDEE8D8B6", // deflate
        "F3E6E53A8E84A61A987A13B0683A3673C1235696DC209DC90013ECEFC4C470EC", // fall_away
    ];

    [Fact]
    public void Grock_table_matches_reference_transcription()
    {
        var set = AnimationTables.Grock;
        Assert.Equal("Grock", set.Name);
        Assert.Equal((byte)0x10, set.Category);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Grock_ships_underworld_only()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x10, out var grid));
        Assert.False(library.Contains($"{grid.Sheet}00.SPF"));
        Assert.False(library.Contains($"{grid.Sheet}00.MPF"));
        Assert.False(library.Contains($"{grid.Sheet}02.SPF"));
        Assert.False(library.Contains($"{grid.Sheet}02.MPF"));
        Assert.True(library.Contains($"{grid.Sheet}03.SPF"));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Grock_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x10, out var grid));
        var anims = AnimationTables.Grock.Anims;

        var pages = SheetImage.Read(library.Read($"{grid.Sheet}03.SPF")).Frames;
        var palette = VgaPalette.From6Bit(library.Read("LVL600.PAL").AsSpan(0, 768));

        int nFrames = anims.SelectMany(a => a.Frames).Max() + 1;
        AnimationGuard.AssertGridHashes(grid, pages, palette, anims, Shas, "Grock");
    }
}
