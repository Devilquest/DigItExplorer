using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed still document representing a single world-map signpost.</summary>
internal sealed class WorldMapSignStill(PreviewContext context, World world, SignType type,
    IReadOnlyList<string> infoPath) : SpriteStill(context, infoPath)
{
    protected override SpriteRenderResult? ComposeSprite()
        => WorldMapSignCompositor.Compose(Context.Library.TryRead, world, type);
}
