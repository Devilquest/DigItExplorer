using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels.Layers;

/// <summary>Manages the layer tree and visibility settings for composed sprite mechanisms.</summary>
internal sealed class SpriteLayerForest(SessionSettings settings) : LayerForest(settings)
{
    private const string ScopeName = "Sprite";

    protected override string Scope => ScopeName;

    public override bool SameShapeAs(LayerForest other) => other is SpriteLayerForest;

    public override List<LayerNode> Build()
    {
        var baseChildren = new List<LayerNode>
        {
            LayerNode.Leaf("Graphic", MapLayer.SpriteGraphic),
            LayerNode.Leaf("Collision", MapLayer.SpriteCollision).Keyed(LayerShortcutGroup.Collision),
        };
        return [BaseGroup(baseChildren)];
    }

    /// <summary>Extracts composed sprite render options from session settings.</summary>
    internal static SpriteRenderOptions RenderOptionsFrom(SessionSettings s)
        => new(s.IsShown(MapLayer.SpriteGraphic), s.IsShown(MapLayer.SpriteCollision));
}
