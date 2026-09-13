using CommunityToolkit.Mvvm.ComponentModel;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.Models;

/// <summary>Tree node in the Layers panel representing a group or individual toggleable layer.</summary>
internal sealed class LayerNode : ObservableObject
{
    /// <summary>Text shown in the Layers panel tree.</summary>
    public string Label { get; }

    /// <summary>Base category or layer name used for summary status readouts.</summary>
    public string Name { get; set; }

    /// <summary>Whether child layer names are listed individually in status bars.</summary>
    public bool ExpandsInInfoBar { get; set; }

    /// <summary>Controlled structural map layer, or null for entity nodes.</summary>
    public MapLayer? Layer { get; }

    /// <summary>DLF entity category byte controlled by this node.</summary>
    public byte? Category { get; }

    /// <summary>DLF entity subtype identifier controlled by this node.</summary>
    public ushort? Type { get; }

    /// <summary>Entity key whose collision footprint is controlled by this node.</summary>
    public (byte Category, ushort? Type)? Collider { get; private init; }

    /// <summary>Whether this layer checkbox is interactive.</summary>
    public bool IsEnabled { get; }

    /// <summary>Whether this node can be collapsed in tree views.</summary>
    public bool CanCollapse { get; set; } = true;

    /// <summary>Whether child layers are mutually exclusive.</summary>
    public bool IsExclusiveGroup { get; set; }

    /// <summary>The layer group whose shortcut key presses this node, or null when no key names it.</summary>
    public LayerShortcutGroup? ShortcutGroup { get; private set; }

    /// <summary>Child layer nodes contained in this group.</summary>
    public List<LayerNode> Children { get; } = new();

    /// <summary>Parent group node, or null for root entries.</summary>
    public LayerNode? Parent { get; private set; }

    /// <summary>Set via <see cref="BindChanged"/> once the whole forest is built; invoked once per user
    /// toggle with the node pressed and the state asked for.</summary>
    private Action<LayerNode, bool>? _changed;

    private bool? _isChecked;

    private LayerNode(string label, MapLayer? layer, byte? category, ushort? type, bool isEnabled = true)
    {
        Label = label;
        Name = label;
        Layer = layer;
        Category = category;
        Type = type;
        IsEnabled = isEnabled;
    }

    /// <summary>Creates a group layer node aggregating child states.</summary>
    public static LayerNode Group(string label, params LayerNode[] children)
    {
        var group = new LayerNode(label, layer: null, category: null, type: null);
        foreach (var child in children)
        {
            child.Parent = group;
            group.Children.Add(child);
        }
        group._isChecked = Aggregate(children);
        return group;
    }

    /// <summary>Creates a structural map layer leaf node.</summary>
    public static LayerNode Leaf(string label, MapLayer layer) => new(label, layer, category: null, type: null);

    /// <summary>Creates an entity category leaf node.</summary>
    public static LayerNode CategoryLeaf(string label, byte category) => new(label, layer: null, category, type: null);

    /// <summary>Creates an entity subtype leaf node.</summary>
    public static LayerNode SubtypeLeaf(string label, byte category, ushort type)
        => new(label, layer: null, category, type);

    /// <summary>Creates a collision footprint leaf node.</summary>
    public static LayerNode ColliderLeaf(string label, (byte Category, ushort? Type) key)
        => new(label, layer: null, category: null, type: null) { Collider = key };

    /// <summary>Creates an inactive placeholder layer node.</summary>
    public static LayerNode Disabled(string label)
        => new(label, layer: null, category: null, type: null, isEnabled: false) { _isChecked = false };

    /// <summary>Marks this node as the one its layer group's shortcut key presses.</summary>
    public LayerNode Keyed(LayerShortcutGroup group)
    {
        ShortcutGroup = group;
        return this;
    }

    /// <summary>Finds the node in this subtree that the given group's shortcut key presses, or null.</summary>
    public LayerNode? ShortcutNode(LayerShortcutGroup group)
    {
        if (ShortcutGroup == group) return this;
        foreach (var child in Children)
            if (child.ShortcutNode(group) is { } found) return found;
        return null;
    }

    public bool? IsChecked
    {
        get => _isChecked;
        set
        {
            if (!IsEnabled) return;

            if (IsExclusiveGroup && value == true)
            {
                LayerNode? toKeep = null;
                foreach (var child in Children)
                    if (child.IsChecked == true) { toKeep = child; break; }
                toKeep ??= Children.Count > 0 ? Children[0] : null;
                foreach (var child in Children)
                    child.SetCheckedRecursive(child == toKeep);
                RecomputeFromChildren();
                _changed?.Invoke(this, true);
                return;
            }

            SetCheckedRecursive(value ?? false);
            if (value == true && Parent is { IsExclusiveGroup: true } exclusiveParent)
            {
                foreach (var sibling in exclusiveParent.Children)
                    if (sibling != this && sibling.IsChecked != false)
                        sibling.SetCheckedRecursive(false); // Uncheck siblings in exclusive parent group.
            }
            Parent?.RecomputeFromChildren();
            _changed?.Invoke(this, value ?? false);
        }
    }

    /// <summary>Recursively sets checked state across matching subtree leaves.</summary>
    public bool SetSubtreeChecked(bool value, Func<LayerNode, bool> include)
    {
        if (Children.Count == 0)
            return IsEnabled && include(this) && SetCheckedRecursive(value);

        if (IsExclusiveGroup && value)
        {
            var candidates = Children.Where(c => c.IsEnabled && include(c)).ToList();
            if (candidates.Count == 0) return false;
            var keep = candidates.FirstOrDefault(c => c.IsChecked == true) ?? candidates[0];
            bool exclusiveChanged = false;
            foreach (var child in Children)
                exclusiveChanged |= child.SetCheckedRecursive(child == keep);
            return RecomputeSelf() | exclusiveChanged;
        }

        bool changed = false;
        foreach (var child in Children)
            changed |= child.SetSubtreeChecked(value, include);
        return RecomputeSelf() | changed;
    }

    private bool SetCheckedRecursive(bool value)
    {
        bool changed = SetProperty(ref _isChecked, value, nameof(IsChecked));
        foreach (var child in Children) changed |= child.SetCheckedRecursive(value);
        return changed;
    }

    private void RecomputeFromChildren()
    {
        RecomputeSelf();
        Parent?.RecomputeFromChildren();
    }

    private bool RecomputeSelf() => SetProperty(ref _isChecked, Aggregate(Children), nameof(IsChecked));

    private static bool? Aggregate(IReadOnlyList<LayerNode> nodes)
    {
        if (nodes.Count == 0) return true;
        if (nodes.All(n => n.IsChecked == true)) return true;
        if (nodes.All(n => n.IsChecked == false)) return false;
        return null; // indeterminate: some checked, some not
    }

    /// <summary>Group names from the tree's root down to this node's own group, which is what a remembered
    /// group choice is keyed by.</summary>
    public IReadOnlyList<string> GroupPath()
    {
        var path = new List<string>();
        for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent) path.Add(ancestor.Name);
        path.Reverse();
        return path;
    }

    /// <summary>Fills every leaf below with the state it opens with and recomputes the groups above them.</summary>
    /// <param name="opens">What a leaf shows, given the leaf and the group path it sits under.</param>
    /// <param name="path">Group names from the tree's root down to this node.</param>
    public void Open(Func<LayerNode, IReadOnlyList<string>, bool> opens, IReadOnlyList<string> path)
    {
        if (Children.Count == 0)
        {
            SetProperty(ref _isChecked, IsEnabled && opens(this, path), nameof(IsChecked));
            return;
        }

        var childPath = new List<string>(path) { Name };
        foreach (var child in Children) child.Open(opens, childPath);
        // An inherited choice can reach several children of a group that shows one member at a time.
        if (IsExclusiveGroup && Children.Count(c => c.IsChecked == true) > 1)
        {
            bool kept = false;
            foreach (var child in Children)
            {
                if (child.IsChecked != true) continue;
                if (kept) child.SetCheckedRecursive(false);
                kept = true;
            }
        }
        RecomputeSelf();
    }

    /// <summary>Wires every node in this subtree to invoke <paramref name="onChange"/> after a toggle.</summary>
    public void BindChanged(Action<LayerNode, bool> onChange)
    {
        _changed = onChange;
        foreach (var child in Children) child.BindChanged(onChange);
    }

    /// <summary>Enumerates all checked structural layers in the subtree.</summary>
    public IEnumerable<MapLayer> VisibleLayers()
    {
        if (Layer is { } layer && IsChecked == true) yield return layer;
        foreach (var child in Children)
            foreach (var l in child.VisibleLayers())
                yield return l;
    }

    /// <summary>Enumerates every structural layer the subtree contains, checked or not.</summary>
    public IEnumerable<MapLayer> ContainedLayers()
    {
        if (Layer is { } layer) yield return layer;
        foreach (var child in Children)
            foreach (var l in child.ContainedLayers())
                yield return l;
    }

    /// <summary>Enumerates all checked entity category and type keys in the subtree.</summary>
    public IEnumerable<(byte Category, ushort? Type)> VisibleEntityKeys()
    {
        if (Category is { } category && IsChecked != false) yield return (category, Type);
        foreach (var child in Children)
            foreach (var k in child.VisibleEntityKeys())
                yield return k;
    }

    /// <summary>Enumerates all checked collision footprint keys in the subtree.</summary>
    public IEnumerable<(byte Category, ushort? Type)> VisibleColliderKeys()
    {
        if (Collider is { } key && IsChecked != false) yield return key;
        foreach (var child in Children)
            foreach (var k in child.VisibleColliderKeys())
                yield return k;
    }

    private bool _isVisible = true;

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    private bool _isExpanded = true;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (!value && !CanCollapse)
            {
                OnPropertyChanged();
                return;
            }
            SetProperty(ref _isExpanded, value);
        }
    }

    /// <summary>Updates visibility based on a search filter query.</summary>
    public bool UpdateVisibility(string query, bool forceVisible = false)
    {
        if (string.IsNullOrEmpty(query))
        {
            IsVisible = true;
            foreach (var child in Children)
            {
                child.UpdateVisibility(query, forceVisible: false);
            }
            return true;
        }

        if (forceVisible)
        {
            IsVisible = true;
            foreach (var child in Children)
            {
                child.UpdateVisibility(query, forceVisible: true);
            }
            return true;
        }

        bool matchSelf = Label.Contains(query, StringComparison.OrdinalIgnoreCase);
        bool matchChildren = false;

        foreach (var child in Children)
        {
            if (child.UpdateVisibility(query, forceVisible: matchSelf))
            {
                matchChildren = true;
            }
        }

        IsVisible = matchSelf || matchChildren;
        return IsVisible;
    }
}
