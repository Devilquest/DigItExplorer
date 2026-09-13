using DigItExplorer.Core.Maps;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed still document representing a single dig spot marker state.</summary>
internal sealed class DigSpotStill(PreviewContext context, DigSpotState state,
    IReadOnlyList<string> infoPath) : SpriteStill(context, infoPath)
{
    protected override SpriteRenderResult? ComposeSprite() => DigSpotCompositor.Compose(Context.Library.TryRead, state);
}
