using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.Core.Slabs;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels.Layers;

/// <summary>Manages the layer tree and visibility settings for Instructions and Credits slab screens.</summary>
internal sealed class SlabLayerForest(SessionSettings settings, bool hasBackground) : LayerForest(settings)
{
    private const string ScopeName = "Slab";

    protected override string Scope => ScopeName;

    public override bool SameShapeAs(LayerForest other)
        => other is SlabLayerForest slab && slab.HasBackground == hasBackground;

    private bool HasBackground => hasBackground;

    public override List<LayerNode> Build()
    {
        var baseChildren = new List<LayerNode>();
        if (hasBackground)
            baseChildren.Add(LayerNode.Leaf("Background", MapLayer.SlabBackground));
        baseChildren.Add(LayerNode.Leaf("Foreground", MapLayer.SlabForeground));
        baseChildren.Add(LayerNode.Leaf("Frame", MapLayer.SlabFrame));
        baseChildren.Add(LayerNode.Leaf("Text", MapLayer.SlabText));
        return [BaseGroup(baseChildren)];
    }

    /// <summary>Extracts slab screen render options from session settings.</summary>
    internal static SlabRenderOptions RenderOptionsFrom(SessionSettings s)
        => new(s.IsShown(MapLayer.SlabBackground), s.IsShown(MapLayer.SlabForeground),
            s.IsShown(MapLayer.SlabFrame), s.IsShown(MapLayer.SlabText));
}
