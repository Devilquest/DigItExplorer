using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels.Layers;

/// <summary>Manages the layer tree and visibility settings for World Map level select screens.</summary>
internal sealed class WorldMapLayerForest : LayerForest
{
    private const string ScopeName = "WorldMap";

    private readonly bool _hasSky;
    private readonly bool _hasBackground;
    private readonly bool _hasPath;
    private readonly HashSet<SignType> _signTypes;

    internal WorldMapLayerForest(SessionSettings settings, bool hasSky, bool hasBackground, bool hasPath,
        IReadOnlySet<SignType> signTypes) : base(settings)
    {
        _hasSky = hasSky;
        _hasBackground = hasBackground;
        _hasPath = hasPath;
        _signTypes = new HashSet<SignType>(signTypes);
    }

    protected override string Scope => ScopeName;

    public override bool SameShapeAs(LayerForest other)
        => other is WorldMapLayerForest map
            && map._hasSky == _hasSky
            && map._hasBackground == _hasBackground
            && map._hasPath == _hasPath
            && map._signTypes.SetEquals(_signTypes);

    public override List<LayerNode> Build()
    {
        var baseChildren = new List<LayerNode>();
        if (_hasSky)
            baseChildren.Add(LayerNode.Leaf("Sky", MapLayer.WorldMapSky));
        if (_hasBackground)
            baseChildren.Add(LayerNode.Leaf("Background", MapLayer.WorldMapBackground));
        baseChildren.Add(LayerNode.Leaf("Front", MapLayer.WorldMapFront));
        var roots = new List<LayerNode> { BaseGroup(baseChildren) };

        if (_hasPath)
            roots.Add(LayerNode.Leaf("Path", MapLayer.WorldMapPath));

        var signChildren = new List<LayerNode>();
        if (_signTypes.Contains(SignType.Level))
            signChildren.Add(LayerNode.Leaf("Level Complete", MapLayer.WorldMapSignLevel));
        if (_signTypes.Contains(SignType.Checkpoint))
            signChildren.Add(LayerNode.Leaf("Check Point", MapLayer.WorldMapSignCheckpoint));
        if (_signTypes.Contains(SignType.Gate))
            signChildren.Add(LayerNode.Leaf("Connection Cave", MapLayer.WorldMapSignGate));
        if (_signTypes.Contains(SignType.Trace))
            signChildren.Add(LayerNode.Leaf("Traces of Dugette", MapLayer.WorldMapSignTrace));
        if (signChildren.Count > 0)
            roots.Add(LayerNode.Group("Signs", signChildren.ToArray()));

        return roots;
    }

    /// <summary>Extracts world map render options from session settings.</summary>
    internal static WorldMapRenderOptions RenderOptionsFrom(SessionSettings s)
        => new(Visible(s, MapLayer.WorldMapSky), Visible(s, MapLayer.WorldMapBackground),
            Visible(s, MapLayer.WorldMapFront), Visible(s, MapLayer.WorldMapPath),
            Visible(s, MapLayer.WorldMapSignLevel), Visible(s, MapLayer.WorldMapSignCheckpoint),
            Visible(s, MapLayer.WorldMapSignGate), Visible(s, MapLayer.WorldMapSignTrace));

    private static bool Visible(SessionSettings s, MapLayer layer) => s.IsShown(layer);
}
