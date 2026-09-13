using System.Linq;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels.Layers;

/// <summary>Manages the layer tree and visibility settings for level maps, collision overlays, and entity categories.</summary>
internal sealed class MapLayerForest : LayerForest
{
    private const string ScopeName = "Map";

    // Entity buckets in display order, placing collision-contributing mechanisms directly under Base.
    private static readonly (string Label, EntityBucket Bucket, LayerShortcutGroup? Key)[] Buckets =
    [
        ("Mechanisms", EntityBucket.Mechanisms, null),
        ("Enemies", EntityBucket.Enemies, LayerShortcutGroup.Enemies),
        ("Goodies", EntityBucket.Goodies, LayerShortcutGroup.Goodies),
        ("Decor", EntityBucket.Decor, null),
        ("Markers", EntityBucket.Markers, null),
        ("Misc & Unknown", EntityBucket.Misc, null),
    ];

    private readonly EntityNames? _entityNames;
    private readonly bool _hasCollision;
    private readonly bool _hasBonusDestinations;
    private readonly bool _hasExitDestinations;
    private readonly IReadOnlyList<DlfRecord> _entities;
    private readonly HashSet<(byte Category, ushort? Type)> _keys;
    private readonly List<(byte Category, ushort? Type)> _colliderKeys;

    internal MapLayerForest(SessionSettings settings, EntityNames? entityNames, bool hasCollision,
        IReadOnlyList<DlfRecord> entities, IEnumerable<(byte Category, ushort? Type)> colliderKeys,
        bool hasBonusDestinations, bool hasExitDestinations) : base(settings)
    {
        _entityNames = entityNames;
        _hasCollision = hasCollision;
        _hasBonusDestinations = hasBonusDestinations;
        _hasExitDestinations = hasExitDestinations;
        _entities = entities;
        _keys = entities.Select(EntityCategories.KeyOf).ToHashSet();
        // Order matches the Mechanisms bucket sprite sequence.
        _colliderKeys = [.. colliderKeys.Distinct().OrderBy(k => (k.Category, k.Type ?? 0))];
    }

    protected override string Scope => ScopeName;

    protected override IEnumerable<(byte Category, ushort? Type)> EntityKeys => _keys;

    protected override IEnumerable<(byte Category, ushort? Type)> ColliderKeys => _colliderKeys;

    public override bool SameShapeAs(LayerForest other)
        => other is MapLayerForest map && map._hasCollision == _hasCollision && map._keys.SetEquals(_keys)
            && map._colliderKeys.SequenceEqual(_colliderKeys)
            && map._hasBonusDestinations == _hasBonusDestinations
            && map._hasExitDestinations == _hasExitDestinations;

    public override List<LayerNode> Build()
    {
        var baseChildren = new List<LayerNode> { LayerNode.Leaf("Terrain", MapLayer.Terrain) };
        if (_hasCollision)
            baseChildren.Add(CollisionGroup());
        var roots = new List<LayerNode> { BaseGroup(baseChildren) };

        foreach (var (label, bucket, key) in Buckets)
        {
            var node = BuildBucket(label, bucket);
            if (node.Children.Count > 0)
                roots.Add(key is { } group ? node.Keyed(group) : node);
        }

        var infoChildren = new List<LayerNode>();
        if (_hasBonusDestinations)
        {
            infoChildren.Add(LayerNode.Leaf("Bonus Destination Info", MapLayer.BonusInfo));
        }
        if (_hasExitDestinations)
        {
            infoChildren.Add(LayerNode.Leaf("Exit Destination Info", MapLayer.ExitInfo));
        }

        if (infoChildren.Count > 0)
        {
            roots.Add(LayerNode.Group("Info Overlays", infoChildren.ToArray()));
        }

        return roots;
    }

    /// <summary>Constructs the collision plane layer group and mechanism footprint leaves.</summary>
    private LayerNode CollisionGroup()
    {
        var children = new List<LayerNode> { LayerNode.Leaf("Terrain", MapLayer.Collision) };
        foreach (var key in _colliderKeys)
        {
            string label = EntityCategories.SubtypeNameOf(key.Category, key.Type ?? 0, _entityNames)
                ?? EntityCategories.LabelOf(key.Category, _entityNames);
            children.Add(LayerNode.ColliderLeaf(label, key));
        }
        return LayerNode.Group("Collision", children.ToArray()).Keyed(LayerShortcutGroup.Collision);
    }

    /// <summary>Extracts level map render options from session settings and active entity keys.</summary>
    internal static MapRenderOptions RenderOptionsFrom(SessionSettings settings,
        IReadOnlySet<(byte Category, ushort? Type)> visibleKeys,
        IReadOnlySet<(byte Category, ushort? Type)> visibleColliderKeys)
        => new(settings.IsShown(MapLayer.Terrain), settings.IsShown(MapLayer.Collision),
            settings.IsShown(MapLayer.ExitInfo), settings.IsShown(MapLayer.BonusInfo), visibleKeys,
            visibleColliderKeys);

    /// <summary>Constructs a layer tree group for the specified entity bucket.</summary>
    private LayerNode BuildBucket(string label, EntityBucket bucket)
    {
        var present = _entities.Where(e => EntityCategories.BucketOf(e.Category) == bucket).ToList();
        if (present.Count == 0)
            return LayerNode.Disabled($"{label} (0)");

        var children = new List<((int Anchor, int WithinFamily) Order, string SortKey, LayerNode Node)>();
        foreach (var catGroup in present.GroupBy(e => e.Category))
        {
            if (EntityCategories.HasSubtypeBreakdown(catGroup.Key))
            {
                foreach (var typeGroup in catGroup.GroupBy(e => EntityCategories.KeyOf(e).Type ?? e.Type))
                {
                    ushort typeKey = typeGroup.Key;
                    string subtypeLabel = EntityCategories.SubtypeNameOf(catGroup.Key, typeKey, _entityNames)
                        ?? $"{EntityCategories.LabelOf(catGroup.Key, _entityNames)} type {typeKey}";
                    var order = DisplayOrderOf(bucket, catGroup.Key, typeKey);
                    children.Add((order, subtypeLabel,
                        LayerNode.SubtypeLeaf($"{subtypeLabel} ({typeGroup.Count()})", catGroup.Key, typeKey)));
                }
            }
            else
            {
                string catLabel = EntityCategories.LabelOf(catGroup.Key, _entityNames);
                var order = DisplayOrderOf(bucket, catGroup.Key, type: null);
                children.Add((order, catLabel,
                    LayerNode.CategoryLeaf($"{catLabel} ({catGroup.Count()})", catGroup.Key)));
            }
        }
        var ordered = HasCuratedOrder(bucket)
            ? children.OrderBy(c => c.Order).Select(c => c.Node).ToList()
            : children.OrderBy(c => c.SortKey, StringComparer.OrdinalIgnoreCase).Select(c => c.Node).ToList();
        var group = LayerNode.Group($"{label} ({present.Count})", ordered.ToArray());
        group.Name = label; // Display name for info bar.
        return group;
    }

    private static bool HasCuratedOrder(EntityBucket bucket)
        => bucket is EntityBucket.Enemies or EntityBucket.Mechanisms;

    private static (int Anchor, int WithinFamily) DisplayOrderOf(EntityBucket bucket, byte category, ushort? type)
        => bucket switch
        {
            EntityBucket.Enemies => EntityCategories.EnemiesDisplayOrderOf(category, type),
            EntityBucket.Mechanisms => (category, type ?? 0),
            _ => default,
        };
}
