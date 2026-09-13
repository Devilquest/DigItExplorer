using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Tests;

/// <summary>Verifies world map layer toggling and the coverage plane that exports as transparency.</summary>
public class WorldMapRendererTests
{
    private static WorldMapRenderOptions Only(bool sky = false, bool background = false, bool front = false,
        bool path = false)
        => new(sky, background, front, path, ShowLevelSigns: false, ShowCheckpointSigns: false,
            ShowGateSigns: false, ShowTraceSigns: false);

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Path_only_keeps_the_full_map_size_and_covers_nothing_but_the_path()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(TestPaths.TryGetGameData(out var data));

        using var library = ResourceLibrary.Open(gameDir);
        var doc = WorldMapDocument.Load(World.Caves, library.TryRead, data.Nodes);
        Assert.NotNull(doc);
        Assert.NotNull(doc!.Path);

        var result = WorldMapRenderer.Render(doc, Only(path: true));

        Assert.NotNull(result);
        Assert.Equal((doc.Width, doc.Height), (result!.Width, result.Height));
        Assert.Equal(doc.Width * doc.Height, result.Alpha.Length);
        Assert.Contains<byte>(255, result.Alpha); // the path pixels
        Assert.Contains<byte>(0, result.Alpha);   // everything the hidden layers would have filled
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Front_layer_leaves_its_transparent_pixels_uncovered()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(TestPaths.TryGetGameData(out var data));

        using var library = ResourceLibrary.Open(gameDir);
        var doc = WorldMapDocument.Load(World.Caves, library.TryRead, data.Nodes);
        Assert.NotNull(doc);
        Assert.NotNull(doc!.Front.Alpha); // caves ships the masked front variant

        var result = WorldMapRenderer.Render(doc, Only(front: true));

        Assert.NotNull(result);
        for (int i = 0; i < result!.Alpha.Length; i++)
            Assert.Equal(doc.Front.Alpha![i] == 0 ? 0 : 255, result.Alpha[i]);
    }
}
