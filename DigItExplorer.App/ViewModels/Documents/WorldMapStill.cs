using DigItExplorer.App.ViewModels.Layers;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed still document representing a world map level select screen.</summary>
internal sealed class WorldMapStill(PreviewContext context, WorldMapDocument document, IReadOnlyList<string> infoPath)
    : MapFamilyStill(context)
{
    /// <summary>Document subject name displayed on empty placeholder views.</summary>
    protected override string PlaceholderSubject => GameKnowledge.WorldMapPrefix(document.World);

    protected override IReadOnlyList<string> InfoPath => infoPath;

    protected override ComposedFrame? Compose()
    {
        var options = WorldMapLayerForest.RenderOptionsFrom(Context.Settings);
        var result = WorldMapRenderer.Render(document, options);
        return result is null ? null : new ComposedFrame(result.Rgb, result.Width, result.Height, result.Alpha);
    }
}
