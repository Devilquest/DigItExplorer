using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Bubble animation table mappings and rendered frame hashes against reference files.</summary>
public class BubbleAnimationTests
{
    // Left to right on the sheet, which is largest ring first.
    private static readonly FrameRect[] ExpectedRectsBySliceIndex =
    [
        new(0, 1, 1, 8, 8), new(0, 10, 1, 8, 8), new(0, 19, 1, 8, 8),
    ];

    [Fact]
    public void Bubble_table_matches_expected_shape()
    {
        var set = AnimationTables.Bubble;
        Assert.Equal("Bubble", set.Name);
        Assert.Equal("WO_BUBLE", set.FixedSheet);
        Assert.Equal("LVL200.PAL", set.FixedPalette);
        Assert.Equal(World.Water, set.FixedLocationWorld);
        Assert.True(set.SlicedSheet);
        Assert.Null(set.Rects);
        Assert.Equal(3, set.Anims.Count);

        // Entries run in ascending id order; their frames run the other way. That is the whole point.
        Assert.Equal([2], set.Anims[0].Frames);
        Assert.Equal([1], set.Anims[1].Frames);
        Assert.Equal([0], set.Anims[2].Frames);
        Assert.Equal("Bubble (small)", set.Anims[0].DisplayName);
        Assert.Equal("Bubble (medium)", set.Anims[1].DisplayName);
        Assert.Equal("Bubble (large)", set.Anims[2].DisplayName);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Bubble_rects_derived_from_the_sheet_match_the_documented_geometry()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var path = "WO_BUBLE.SPF";
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains(path), $"{path} is missing from this install");

        var sheet = SheetImage.Read(library.Read(path));
        var rects = SkinCatalog.RectsOf(AnimationTables.Bubble, sheet.Frames)!;

        Assert.Equal(3, rects.Count);
        for (int sliceIndex = 0; sliceIndex < 3; sliceIndex++)
            Assert.Equal(ExpectedRectsBySliceIndex[sliceIndex], rects[sliceIndex]);
    }
}
