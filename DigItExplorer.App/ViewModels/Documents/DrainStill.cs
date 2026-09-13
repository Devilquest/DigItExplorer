using DigItExplorer.App.ViewModels.Layers;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed still document representing a water world bonus drain state.</summary>
internal sealed class DrainStill(PreviewContext context, DrainState state, IReadOnlyList<string> infoPath)
    : SpriteStill(context, infoPath)
{
    protected override SpriteRenderResult? ComposeSprite()
        => DrainCompositor.Compose(Context.Library.TryRead, state,
            SpriteLayerForest.RenderOptionsFrom(Context.Settings));
}
