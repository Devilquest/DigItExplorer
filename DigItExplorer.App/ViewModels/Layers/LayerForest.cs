using System.Linq;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels.Layers;

/// <summary>Abstract base class managing layer hierarchy, shape comparison, and persistence for document layers.</summary>
internal abstract class LayerForest(SessionSettings settings)
{
    /// <summary>Persisted session settings backing layer visibility state.</summary>
    protected SessionSettings Settings { get; } = settings;

    /// <summary>Name separating this document kind's remembered layers from every other kind's.</summary>
    protected abstract string Scope { get; }

    /// <summary>Builds the document layer hierarchy root nodes.</summary>
    public abstract List<LayerNode> Build();

    /// <summary>Determines whether the specified layer forest produces an identical tree shape.</summary>
    public abstract bool SameShapeAs(LayerForest other);

    /// <summary>Entity keys this document contains, whose visibility is remembered per key.</summary>
    protected virtual IEnumerable<(byte Category, ushort? Type)> EntityKeys => [];

    /// <summary>Collision footprint keys this document contains, whose visibility is remembered per key.</summary>
    protected virtual IEnumerable<(byte Category, ushort? Type)> ColliderKeys => [];

    /// <summary>Fills a freshly built tree with what the user last said, so a layer they have never been
    /// shown follows the choice made on the group it appears under.</summary>
    /// <param name="roots">The tree as <see cref="Build"/> left it.</param>
    public void Open(IReadOnlyList<LayerNode> roots)
    {
        foreach (var root in roots) root.Open(OpensWith, []);
    }

    /// <summary>Records the state of everything this document contains and of nothing else, so opening a
    /// document that lacks a layer cannot decide anything about it.</summary>
    /// <param name="roots">The tree as the user left it.</param>
    /// <param name="pressed">The nodes the user acted on, whose groups are the ones they spoke about.</param>
    /// <param name="asked">The state those nodes were asked for.</param>
    public void Persist(IReadOnlyList<LayerNode> roots, IReadOnlyList<LayerNode> pressed, bool asked)
    {
        var visibleLayers = VisibleLayersOf(roots);
        foreach (var layer in ContainedLayersOf(roots))
            Settings.Layers.Record(Scope, layer, visibleLayers.Contains(layer));

        var visibleKeys = VisibleKeysOf(roots);
        foreach (var key in EntityKeys)
            Settings.Layers.Record(StickyLayers.EntityKey(key.Category, key.Type), visibleKeys.Contains(key));

        var visibleColliders = VisibleColliderKeysOf(roots);
        foreach (var key in ColliderKeys)
            Settings.Layers.Record(StickyLayers.ColliderKey(key.Category, key.Type),
                visibleColliders.Contains(key));

        foreach (var node in pressed)
            RecordIntents(node, node.GroupPath(), asked);
    }

    /// <summary>Publishes what the open document draws, which the still renderers read.</summary>
    /// <param name="roots">The tree as the user left it.</param>
    public void Show(IReadOnlyList<LayerNode> roots)
    {
        var visibleLayers = VisibleLayersOf(roots);
        Settings.ShownLayers.Clear();
        foreach (var layer in ContainedLayersOf(roots))
            Settings.ShownLayers[layer] = visibleLayers.Contains(layer);
    }

    /// <summary>Extracts the set of checked structural map layers from tree roots.</summary>
    protected static HashSet<MapLayer> VisibleLayersOf(IReadOnlyList<LayerNode> roots)
        => new(roots.SelectMany(n => n.VisibleLayers()));

    /// <summary>Extracts the set of checked entity keys from tree roots.</summary>
    protected static HashSet<(byte Category, ushort? Type)> VisibleKeysOf(IReadOnlyList<LayerNode> roots)
        => new(roots.SelectMany(n => n.VisibleEntityKeys()));

    /// <summary>Extracts the set of checked collision footprint keys from tree roots.</summary>
    protected static HashSet<(byte Category, ushort? Type)> VisibleColliderKeysOf(IReadOnlyList<LayerNode> roots)
        => new(roots.SelectMany(n => n.VisibleColliderKeys()));

    /// <summary>Constructs a Base layer group configured to expand individual leaves in status bars.</summary>
    protected static LayerNode BaseGroup(List<LayerNode> children)
    {
        var group = LayerNode.Group("Base", children.ToArray());
        group.ExpandsInInfoBar = true;
        return group;
    }

    private static IEnumerable<MapLayer> ContainedLayersOf(IReadOnlyList<LayerNode> roots)
        => roots.SelectMany(n => n.ContainedLayers()).Distinct();

    /// <summary>Resolves one leaf's opening state from its own remembered choice, the groups above it, and
    /// the default for its kind.</summary>
    private bool OpensWith(LayerNode leaf, IReadOnlyList<string> groupPath)
    {
        if (leaf.Layer is { } layer)
            return Settings.Layers.IsVisible(Scope, layer, groupPath);
        if (leaf.Collider is { } collider)
            return Settings.Layers.IsVisible(Scope, StickyLayers.ColliderKey(collider.Category, collider.Type),
                groupPath, fallback: false);
        if (leaf.Category is { } category)
            return Settings.Layers.IsVisible(Scope, StickyLayers.EntityKey(category, leaf.Type), groupPath,
                EntityCategories.DefaultVisible(category));
        return false;
    }

    /// <summary>Records what pressing a node said about every group it turned, which is nothing at all when
    /// the node pressed was a single layer.</summary>
    private void RecordIntents(LayerNode node, IReadOnlyList<string> path, bool asked)
    {
        if (node.Children.Count == 0) return;

        var self = new List<string>(path) { node.Name };
        // A group left partly on was not turned wholesale, so it carries no statement about layers unseen.
        // An exclusive group is the exception: on there means one member on, which is all it can mean.
        if (node.IsExclusiveGroup || node.IsChecked == asked)
            Settings.Layers.RecordIntent(Scope, self, asked);
        foreach (var child in node.Children)
            RecordIntents(child, self, asked);
    }
}
