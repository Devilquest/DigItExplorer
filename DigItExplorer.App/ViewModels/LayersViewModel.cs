using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.App.ViewModels.Layers;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Menu;
using DigItExplorer.Core.Minigames;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels;

/// <summary>ViewModel managing layer tree hierarchies, filtering, expansion, and visibility toggles.</summary>
internal sealed partial class LayersViewModel : ObservableObject
{
    private readonly SessionSettings _settings;

    private LayerForest? _forest;
    private List<LayerNode>? _nodes;

    [ObservableProperty] private List<LayerNode>? _items;
    [ObservableProperty] private bool _isAvailable;
    [ObservableProperty] private string _filterText = string.Empty;

    /// <summary>Occurs when layer visibility state is toggled.</summary>
    public event Action? VisibleLayersChanged;

    private EntityNames? _entityNames;

    internal LayersViewModel(SessionSettings settings) => _settings = settings;

    /// <summary>Attaches entity names for layer labels.</summary>
    public void Attach(EntityNames entityNames) => _entityNames = entityNames;

    /// <summary>Configures layer hierarchy for level maps.</summary>
    public void Ensure(bool hasCollision, IReadOnlyList<DlfRecord> entities,
        IEnumerable<(byte Category, ushort? Type)> colliderKeys, bool hasBonusDestinations,
        bool hasExitDestinations)
        => Adopt(new MapLayerForest(_settings, _entityNames, hasCollision, entities, colliderKeys,
            hasBonusDestinations, hasExitDestinations));

    /// <summary>Configures layer hierarchy for world maps.</summary>
    public void EnsureWorldMap(bool hasSky, bool hasBackground, bool hasPath, IReadOnlySet<SignType> signTypes)
        => Adopt(new WorldMapLayerForest(_settings, hasSky, hasBackground, hasPath, signTypes));

    /// <summary>Configures layer hierarchy for end sequence screens.</summary>
    public void EnsureEndSequence() => Adopt(new EndSequenceLayerForest(_settings));

    /// <summary>Configures layer hierarchy for minigame boards.</summary>
    public void EnsureMinigameBoard(MinigameBoard board) => Adopt(new MinigameLayerForest(_settings, board));

    /// <summary>Configures layer hierarchy for slab screens.</summary>
    public void EnsureSlab(bool hasBackground) => Adopt(new SlabLayerForest(_settings, hasBackground));

    /// <summary>Configures layer hierarchy for composed sprite mechanisms.</summary>
    public void EnsureSprite() => Adopt(new SpriteLayerForest(_settings));

    /// <summary>Configures layer hierarchy for the main menu screen.</summary>
    public void EnsureMainMenu(bool hasBackground, bool hasCollision, IReadOnlyList<DlfRecord> entities,
        bool hasMainSign, bool hasSetup, bool hasPlay, bool hasIntro)
        => Adopt(new MainMenuLayerForest(_settings, _entityNames, hasBackground, hasCollision, entities,
            hasMainSign, hasSetup, hasPlay, hasIntro));

    /// <summary>Installs the specified layer forest and binds toggle events.</summary>
    private void Adopt(LayerForest forest)
    {
        FilterText = string.Empty;

        if (_forest is null || !_forest.SameShapeAs(forest))
        {
            var nodes = forest.Build();
            forest.Open(nodes);
            _forest = forest;
            _nodes = nodes;
            foreach (var root in nodes) root.BindChanged(OnLayerToggled);
            Items = nodes;
        }

        _forest.Show(_nodes!);
        IsAvailable = true;
    }

    /// <summary>Clears active layer nodes and marks panel unavailable.</summary>
    public void Clear()
    {
        _forest = null;
        _nodes = null;
        Items = null;
        IsAvailable = false;
    }

    /// <summary>Detaches entity names and clears active layer forest.</summary>
    public void Detach()
    {
        Clear();
        _entityNames = null;
    }

    private void OnLayerToggled(LayerNode pressed, bool asked) => Apply([pressed], asked);

    /// <summary>Presses the node a layer group's shortcut key names, exactly as clicking its checkbox does.</summary>
    public void ToggleGroup(LayerShortcutGroup group)
    {
        var node = _nodes?.Select(root => root.ShortcutNode(group)).FirstOrDefault(found => found is not null);
        if (node is null || !node.IsEnabled) return;
        // The value a three-state checkbox asks for on a click: ticked goes to indeterminate, which the node
        // resolves to all-off, and a partly ticked group therefore turns off rather than on.
        node.IsChecked = node.IsChecked == true ? null : node.IsChecked.HasValue;
    }

    /// <summary>Records what the user just said, republishes what the document draws, and asks for one
    /// re-render.</summary>
    private void Apply(IReadOnlyList<LayerNode> pressed, bool asked)
    {
        if (_forest is null || _nodes is null) return;
        _forest.Persist(_nodes, pressed, asked);
        _forest.Show(_nodes);
        VisibleLayersChanged?.Invoke();
    }

    private HashSet<(byte Category, ushort? Type)> VisibleEntityKeys()
        => new(_nodes?.SelectMany(n => n.VisibleEntityKeys()) ?? []);

    private HashSet<(byte Category, ushort? Type)> VisibleColliderKeys()
        => new(_nodes?.SelectMany(n => n.VisibleColliderKeys()) ?? []);

    /// <summary>Extracts level map render options from active layer states.</summary>
    public MapRenderOptions BuildRenderOptions()
        => MapLayerForest.RenderOptionsFrom(_settings, VisibleEntityKeys(), VisibleColliderKeys());

    /// <summary>Extracts main menu render options from active layer states.</summary>
    public MainMenuRenderOptions BuildMainMenuRenderOptions()
        => MainMenuLayerForest.RenderOptionsFrom(_settings, VisibleEntityKeys());

    partial void OnFilterTextChanged(string value)
    {
        if (_nodes is null) return;
        var query = value?.Trim() ?? "";
        foreach (var node in _nodes)
            node.UpdateVisibility(query);
    }

    [RelayCommand]
    private void ClearFilter() => FilterText = string.Empty;

    [RelayCommand]
    private void SelectAll() => SetAllChecked(true);

    [RelayCommand]
    private void DeselectAll() => SetAllChecked(false);

    /// <summary>Sets visibility for all matching layer nodes in the active tree.</summary>
    private void SetAllChecked(bool value)
    {
        if (_nodes is null) return;
        bool changed = false;
        foreach (var root in _nodes) changed |= root.SetSubtreeChecked(value, n => n.IsVisible);
        // One command speaks for every group it turned whole, which a filter can leave it short of.
        if (changed) Apply(_nodes, value);
    }

    [RelayCommand]
    private void ExpandAll()
    {
        if (_nodes is null) return;
        foreach (var node in _nodes) SetExpandedRecursive(node, true);
    }

    [RelayCommand]
    private void CollapseAll()
    {
        if (_nodes is null) return;
        foreach (var node in _nodes) SetExpandedRecursive(node, false);
    }

    private static void SetExpandedRecursive(LayerNode node, bool expanded)
    {
        node.IsExpanded = expanded;
        foreach (var child in node.Children)
            SetExpandedRecursive(child, expanded);
    }

    /// <summary>Extracts display names for all currently visible layers.</summary>
    public IReadOnlyList<string> VisibleLayerNames()
    {
        var names = new List<string>();
        foreach (var root in _nodes ?? [])
        {
            if (root.ExpandsInInfoBar)
                names.AddRange(root.Children.Where(c => c.IsChecked != false).Select(c => c.Name));
            else if (root.IsEnabled && root.IsChecked != false)
                names.Add(root.Name);
        }
        return names;
    }
}
