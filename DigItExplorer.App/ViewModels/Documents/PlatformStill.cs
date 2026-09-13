using DigItExplorer.App.Models;
using DigItExplorer.App.ViewModels.Layers;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed still document representing a moving platform sprite cell and collider footprint.</summary>
internal sealed class PlatformStill(PreviewContext context, PlatformRef platform, IReadOnlyList<string> infoPath)
    : SpriteStill(context, infoPath)
{
    protected override SpriteRenderResult? ComposeSprite()
        => PlatformCompositor.Compose(Context.Library.TryRead, platform.World, platform.Cell,
            SpriteLayerForest.RenderOptionsFrom(Context.Settings));
}
