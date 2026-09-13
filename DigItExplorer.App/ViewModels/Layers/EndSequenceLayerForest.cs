using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.Core.Ending;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels.Layers;

/// <summary>Manages the layer tree and visibility settings for End Sequence credit screens.</summary>
internal sealed class EndSequenceLayerForest(SessionSettings settings) : LayerForest(settings)
{
    private const string ScopeName = "EndSequence";

    protected override string Scope => ScopeName;

    public override bool SameShapeAs(LayerForest other) => other is EndSequenceLayerForest;

    public override List<LayerNode> Build()
    {
        var baseChildren = new List<LayerNode>
        {
            LayerNode.Leaf("Background", MapLayer.EndSequenceBackground),
            LayerNode.Leaf("Text", MapLayer.EndSequenceText),
        };
        return [BaseGroup(baseChildren)];
    }

    /// <summary>Extracts End Sequence render options from session settings.</summary>
    internal static EndSequenceRenderOptions RenderOptionsFrom(SessionSettings s)
        => new(s.IsShown(MapLayer.EndSequenceBackground), s.IsShown(MapLayer.EndSequenceText));
}
