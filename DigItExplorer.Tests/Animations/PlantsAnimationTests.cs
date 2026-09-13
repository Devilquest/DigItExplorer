using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Plant animation tables and sheet slice geometry against reference files.</summary>
public class PlantsAnimationTests
{
    private const int Count = 9;

    // The sheet states these itself, so they are a transcription of the artwork rather than of anything in
    // the executable.
    private static readonly FrameRect[] ExpectedRects =
    [
        new(0, 0, 0, 68, 42), new(0, 69, 0, 68, 42), new(0, 138, 0, 75, 46), new(0, 214, 0, 75, 46),
        new(0, 0, 43, 58, 53), new(0, 59, 43, 58, 53), new(0, 118, 47, 50, 33), new(0, 169, 47, 50, 33),
        new(0, 0, 97, 37, 58),
    ];

    // The table builds its labels from EntityCategories, so they are pinned here as well: otherwise a silent
    // change to that shared array would rename nine entries with nothing failing.
    private static readonly string[] ExpectedNames =
    [
        "Vine (gold)", "Vine (silver)", "Berry Bush (gold)", "Berry Bush (green)",
        "Fruit Flower (red)", "Fruit Flower (gold)", "Curl Weed (gold)", "Curl Weed (silver)", "Yellow Blossom",
    ];

    [Fact]
    public void Plant_table_matches_expected_shape()
    {
        var set = AnimationTables.Plant;
        Assert.Equal("Plant", set.Name);
        Assert.Equal("WO_PLANT", set.FixedSheet);
        Assert.Equal("LVL000.PAL", set.FixedPalette);
        Assert.Equal(World.Caves, set.FixedLocationWorld);
        Assert.Null(set.Category);
        Assert.True(set.SlicedSheet);
        Assert.Equal((byte)224, set.SliceSeparator);
        Assert.Null(set.Rects);
        Assert.Equal(Count, set.Anims.Count);

        for (int i = 0; i < Count; i++)
        {
            Assert.Equal([i], set.Anims[i].Frames);
            Assert.Equal(AnimMode.Pose, set.Anims[i].Mode);
            Assert.Equal(ExpectedNames[i], set.Anims[i].DisplayName);
            Assert.Equal(ExpectedNames[i], SkinCatalog.PrettyAnimName(set.Name, set.Anims[i]));
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Plant_rects_derived_from_the_sheet_match_the_documented_geometry()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var path = "WO_PLANT.SPF";
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains(path), $"{path} is missing from this install");

        var sheet = SheetImage.Read(library.Read(path));
        var rects = SkinCatalog.RectsOf(AnimationTables.Plant, sheet.Frames)!;

        Assert.Equal(Count, rects.Count);
        for (int i = 0; i < Count; i++)
            Assert.Equal(ExpectedRects[i], rects[i]);
    }
}
