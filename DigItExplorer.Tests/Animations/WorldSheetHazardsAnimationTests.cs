using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Falling Rock and Nirp Egg world-sheet hazard animation tables against reference files.</summary>
public class WorldSheetHazardsAnimationTests
{
    [Fact]
    public void FallingRock_table_matches_expected_shape()
    {
        var set = AnimationTables.FallingRock;
        Assert.Equal("Falling Rock", set.Name);
        Assert.Null(set.Category);
        Assert.NotNull(set.WorldSheet);
        Assert.Equal("WO_RCK{0}", set.WorldSheet!.SheetFormat);
        Assert.True(set.SlicedSheet);
        Assert.Null(set.Rects);
        Assert.Single(set.Anims);
        Assert.Equal([0], set.Anims[0].Frames);
    }

    [Fact]
    public void NirpEgg_table_matches_expected_shape()
    {
        var set = AnimationTables.NirpEgg;
        Assert.Equal("Nirp Egg", set.Name);
        Assert.Null(set.Category);
        Assert.NotNull(set.WorldSheet);
        Assert.Equal("WO_NRP{0}", set.WorldSheet!.SheetFormat);
        Assert.False(set.SlicedSheet);
        Assert.Single(set.Anims);
        Assert.Equal(new FrameRect(0, 0, 168, 13, 7), set.Rects![0]);
    }

    /// <summary>Guards that world-sheet hazard resolution tolerates missing optional world suffixes.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.SupportedBuild)]
    [InlineData("Falling Rock")]
    [InlineData("Nirp Egg")]
    public void Resolves_exactly_the_suffixes_this_install_ships(string name)
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(TestPaths.TryGetGameData(out var data));

        var catalog = new SkinCatalog(library.TryRead, data.Nodes, data.EntityNames);

        var set = AnimationTables.All[name];
        var skins = catalog.SkinsOf(set);

        Assert.Equal(2, skins.Count);
        Assert.Equal(["00", "02"], skins.Select(s => s.Suffix));
        Assert.All(skins, s => Assert.True(s.Present[0], $"{name}/{s.Label}: expected art"));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void FallingRock_rects_derived_from_the_sheet_are_the_whole_rock_cell()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var path = "WO_RCK00.SPF";
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains(path), $"{path} is missing from this install");

        var sheet = SheetImage.Read(library.Read(path));
        var rects = SkinCatalog.RectsOf(AnimationTables.FallingRock, sheet.Frames)!;

        Assert.Single(rects);
        Assert.Equal(new FrameRect(0, 1, 1, 29, 20), rects[0]);
    }
}
