using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies world map document layer assembly across individual game worlds.</summary>
public class WorldMapDocumentTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Boss_screen_has_no_sky_or_bk_and_falls_back_to_the_flattened_base_for_front()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(TestPaths.TryGetGameData(out var data));

        var doc = WorldMapDocument.Load(World.Boss, library.TryRead, data.Nodes);

        Assert.NotNull(doc);
        Assert.Null(doc!.Sky);
        Assert.Null(doc.Background);
        Assert.Equal((320, 200), (doc.Front.Width, doc.Front.Height));
        Assert.Null(doc.Front.Alpha); // the LD fallback has no transparency concept: fully opaque
        Assert.Null(doc.SignSheet); // MAP04 ships no SGN sheet
        Assert.NotNull(doc.Path); // MAP04 does ship a single-node path plane
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Caves_has_sky_masked_bk_and_a_transparent_front()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(TestPaths.TryGetGameData(out var data));

        var doc = WorldMapDocument.Load(World.Caves, library.TryRead, data.Nodes);

        Assert.NotNull(doc);
        Assert.NotNull(doc!.Sky);
        Assert.NotNull(doc.Background);
        Assert.NotNull(doc.Background!.Alpha); // caves ships the masked BKF+BKM variant
        Assert.NotNull(doc.Front.Alpha);
        Assert.Equal((960, 200), (doc.Front.Width, doc.Front.Height));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Water_has_no_sky_and_an_unmasked_opaque_bk()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(TestPaths.TryGetGameData(out var data));

        var doc = WorldMapDocument.Load(World.Water, library.TryRead, data.Nodes);

        Assert.NotNull(doc);
        Assert.Null(doc!.Sky); // water ships no SKY.SPF
        Assert.NotNull(doc.Background);
        Assert.Null(doc.Background!.Alpha); // water's single unmasked BK.SPF is fully opaque
    }
}
