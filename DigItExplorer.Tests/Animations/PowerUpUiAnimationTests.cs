using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies Power-up Banners and Status Icons animation tables against WO_G&amp;S.SPF.</summary>
public class PowerUpUiAnimationTests
{
    [Fact]
    public void PowerUpBanners_table_matches_expected_shape()
    {
        var set = AnimationTables.PowerUpBanners;
        Assert.Equal("Power-up Banners", set.Name);
        Assert.Equal("WO_G&S", set.FixedSheet);
        Assert.True(set.SlicedSheet);
        Assert.Null(set.Rects);

        var expected = new (int Frame, string Label)[]
        {
            (13, "Spring Boots"), (14, "Rapid Fire"), (15, "Jet Pack"), (16, "Super Dug"), (22, "Cheat Code"),
        };
        Assert.Equal(expected.Length, set.Anims.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal([expected[i].Frame], set.Anims[i].Frames);
            Assert.Equal(expected[i].Label, set.Anims[i].DisplayName);
        }
    }

    [Fact]
    public void StatusIcons_table_matches_expected_shape_and_dispatcher_order()
    {
        var set = AnimationTables.StatusIcons;
        Assert.Equal("Status Icons", set.Name);
        Assert.Equal("WO_G&S", set.FixedSheet);
        Assert.True(set.SlicedSheet);
        Assert.Null(set.Rects);

        var expected = new (int Frame, string Label)[]
        {
            (17, "Spring Boots"), (18, "Rapid Fire"), (19, "Jet Pack"), (20, "Time"), (21, "Super Dug"),
        };
        Assert.Equal(expected.Length, set.Anims.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal([expected[i].Frame], set.Anims[i].Frames);
            Assert.Equal(expected[i].Label, set.Anims[i].DisplayName);
        }
    }

    /// <summary>Verifies that WO_G&amp;S slices into exactly 23 distinct sprites across all UI categories.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void WO_GS_slices_to_23_cells_total()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var path = "WO_G&S.SPF";
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains(path), $"{path} is missing from this install");

        var sheet = SheetImage.Read(library.Read(path));
        var cells = SpriteSheetSlicer.Slice(sheet.Frames[0], sheet.Frames[0][0]);

        Assert.Equal(23, cells.Count);
    }
}
