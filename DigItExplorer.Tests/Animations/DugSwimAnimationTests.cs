using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Dug water swimming animation tables and frame hashes against reference files.</summary>
public class DugSwimAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("swim", [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], AnimMode.Loop),
        ("swim_left_map", [12, 13, 14, 15, 16, 17, 18, 19, 20, 21], AnimMode.Loop),
        ("swim_turn", [24, 25, 26], AnimMode.Once),
    ];

    private static readonly string[] Shas =
    [
        "E015EDF575A76E34CBEA75D309DCB4F11EDCC64178AE1C5B6813DF80CE9AA404",
        "7ADBA77C9C5037486CA9231DE77342AAB33DE3EDE112040823CBCA0B369EC964",
        "48CA2C33C0EC01F85A939A8CF8E986268C2EC87EAD350ADF9736792BE9E4F176",
    ];

    [Fact]
    public void DugSwim_table_matches_transcription()
    {
        var set = AnimationTables.DugSwim;
        Assert.Equal("DugSwim", set.Name);
        Assert.Null(set.Category);
        Assert.Equal("DUG7A", set.FixedSheet);
        Assert.Equal("LVL200.PAL", set.FixedPalette);
        // Confined to the Water world (LVL200.PAL).
        Assert.Equal(World.Water, set.FixedLocationWorld);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void DugSwim_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.DugSwim;
        var pages = SheetImage.Read(library.Read("DUG7A.SPF")).Frames;
        var palette = VgaPalette.From6Bit(library.Read("LVL200.PAL").AsSpan(0, 768));

        AnimationGuard.AssertRectHashes(set, pages, palette, Shas);
    }
}
