using DigItExplorer.Core.Maps;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed still document representing the water world exit sign.</summary>
internal sealed class ExitSignStill(PreviewContext context, IReadOnlyList<string> infoPath)
    : SpriteStill(context, infoPath)
{
    protected override SpriteRenderResult? ComposeSprite() => ExitSignCompositor.Compose(Context.Library.TryRead);
}
