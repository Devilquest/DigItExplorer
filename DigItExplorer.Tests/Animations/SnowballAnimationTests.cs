using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies the Snowball table (<c>WO_THR02.SPF</c>'s packed strip below Troggi's grid).</summary>
public class SnowballAnimationTests
{
    [Fact]
    public void Snowball_table_matches_expected_shape()
    {
        var set = AnimationTables.Snowball;
        Assert.Equal("Snowball", set.Name);
        Assert.Null(set.Category);
        Assert.NotNull(set.WorldSheet);
        Assert.Equal("WO_THR{0}", set.WorldSheet!.SheetFormat);
        Assert.True(set.SlicedSheet);
        Assert.Equal((byte)150, set.SliceSeparator);
        Assert.Equal((180, 199), set.SliceBand);
        Assert.Null(set.Rects);

        Assert.Equal(2, set.Anims.Count);
        Assert.Equal([0], set.Anims[0].Frames);
        Assert.Null(set.Anims[0].DisplayName);
        Assert.Equal([1], set.Anims[1].Frames);
        Assert.Equal("Snowball pile (unused)", set.Anims[1].DisplayName);
    }

    /// <summary>Verifies that the snowball band slices to 10 cells and cell 0 matches executable registration.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Band_slices_to_exactly_10_cells_and_cell_0_matches_the_registered_snowball_rect()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var path = "WO_THR02.SPF";
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains(path), $"{path} is missing from this install");

        var sheet = SheetImage.Read(library.Read(path));
        var band = SpriteSheetSlicer.Slice(sheet.Frames[0], 150, 180, 199);

        Assert.Equal(10, band.Count);
        // seg3:0x7C4A base 850 id 29: the registered rect (0,180)-(5,184).
        Assert.Equal((0, 180, 6, 5), (band[0].X, band[0].Y, band[0].W, band[0].H));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Snowball_rects_derived_from_the_sheet_are_the_first_two_band_cells()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var path = "WO_THR02.SPF";
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains(path), $"{path} is missing from this install");

        var sheet = SheetImage.Read(library.Read(path));
        var rects = SkinCatalog.RectsOf(AnimationTables.Snowball, sheet.Frames)!;

        Assert.Equal(2, rects.Count);
        Assert.Equal(new FrameRect(0, 0, 180, 6, 5), rects[0]);
        Assert.Equal(new FrameRect(0, 7, 180, 12, 10), rects[1]);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Snowball_resolves_only_the_snow_world_skin()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(TestPaths.TryGetGameData(out var data));

        var catalog = new SkinCatalog(library.TryRead, data.Nodes, data.EntityNames);

        var skins = catalog.SkinsOf(AnimationTables.Snowball);

        Assert.Single(skins);
        Assert.Equal("02", skins[0].Suffix);
        Assert.True(skins[0].Present[0], "expected the snowball to have art");
        Assert.True(skins[0].Present[1], "expected the pile to have art");
    }
}
