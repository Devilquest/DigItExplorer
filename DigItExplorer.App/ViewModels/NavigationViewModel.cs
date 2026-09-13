using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.App.Services.ResourceTree;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.App.ViewModels;

/// <summary>ViewModel managing Raw and Resources navigation trees, search filtering, and node selection.</summary>
internal sealed partial class NavigationViewModel : ObservableObject
{
    private ResourceLibrary? _library;
    private SkinCatalog? _skins;
    private GameData? _data;
    private TreeNode? _selectedNode;
    private List<TreeNode>? _rawRoots;
    private List<TreeNode>? _resourcesRoots;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RawEmpty))]
    private ICollectionView? _rawItems;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResourcesEmpty))]
    private ICollectionView? _resourcesItems;

    [ObservableProperty] private int _selectedTreeTab;

    /// <summary>Whether the active game folder offers cataloged semantic resources.</summary>
    [ObservableProperty] private bool _isResourcesTabAvailable = true;

    /// <summary>The node selected in either tree, or null.</summary>
    public TreeNode? SelectedNode => _selectedNode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RawEmpty))]
    private string _rawFilterText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResourcesEmpty))]
    private string _resourcesFilterText = string.Empty;

    /// <summary>Whether the Raw tree search filter matched zero nodes.</summary>
    public bool RawEmpty => IsEmptySearch(RawFilterText, RawItems);

    /// <summary>Whether the Resources tree search filter matched zero nodes.</summary>
    public bool ResourcesEmpty => IsEmptySearch(ResourcesFilterText, ResourcesItems);

    private static bool IsEmptySearch(string filter, ICollectionView? items)
        => filter.Trim().Length > 0 && (items is null || items.IsEmpty);

    /// <summary>Attaches collaborators and builds navigation tree hierarchies.</summary>
    public void Attach(ResourceLibrary library, SkinCatalog? skins, GameData? data)
    {
        _library = library;
        _skins = skins;
        _data = data;
        RebuildRaw();
        RebuildResources();
        IsResourcesTabAvailable = _resourcesRoots is { Count: > 0 };
        SelectedTreeTab = IsResourcesTabAvailable ? TreeTabs.Resources : TreeTabs.Raw;
    }

    /// <summary>Clears navigation tree hierarchies and detaches collaborators.</summary>
    public void Detach()
    {
        _library = null;
        _skins = null;
        _data = null;
        _selectedNode = null;
        _rawRoots = null;
        _resourcesRoots = null;
        RawItems = null;
        ResourcesItems = null;
        RawFilterText = string.Empty;
        ResourcesFilterText = string.Empty;
        IsResourcesTabAvailable = true;
        SelectedTreeTab = TreeTabs.Resources;
    }

    partial void OnSelectedTreeTabChanged(int value)
    {
        if (value == TreeTabs.Resources && !IsResourcesTabAvailable)
            SelectedTreeTab = TreeTabs.Raw;
    }

    partial void OnIsResourcesTabAvailableChanged(bool value)
    {
        if (!value && SelectedTreeTab == TreeTabs.Resources)
            SelectedTreeTab = TreeTabs.Raw;
    }

    partial void OnRawFilterTextChanged(string value) => SearchRaw();
    partial void OnResourcesFilterTextChanged(string value) => SearchResources();

    private void RebuildRaw()
    {
        if (_library is null)
        {
            _rawRoots = null;
            RawItems = null;
            return;
        }

        _rawRoots = ResourceTreeBuilder.BuildFamilyTree(_library.Names);
        RawItems = ViewOf(_rawRoots);
        SearchRaw();
    }

    private void RebuildResources()
    {
        if (_library is null || _skins is null || _data is null)
        {
            _resourcesRoots = null;
            ResourcesItems = null;
            return;
        }

        _resourcesRoots = ResourceTreeBuilder.BuildResourcesTree(_library, _skins, _data);
        ResourcesItems = ViewOf(_resourcesRoots);
        SearchResources();
    }

    private void SearchRaw() => Search(_rawRoots, RawFilterText, RawItems, nameof(RawEmpty));

    private void SearchResources()
        => Search(_resourcesRoots, ResourcesFilterText, ResourcesItems, nameof(ResourcesEmpty));

    private void Search(List<TreeNode>? roots, string filterText, ICollectionView? view, string emptyProperty)
    {
        if (roots is null) return;
        TreeSearch.Apply(roots, filterText.Trim());
        view?.Refresh();
        OnPropertyChanged(emptyProperty);
    }

    private static ICollectionView ViewOf(List<TreeNode> roots)
    {
        var view = CollectionViewSource.GetDefaultView(roots);
        view.Filter = node => ((TreeNode)node).IsShown;
        return view;
    }

    [RelayCommand]
    private void ClearRawFilter() => RawFilterText = string.Empty;

    [RelayCommand]
    private void ClearResourcesFilter() => ResourcesFilterText = string.Empty;

    [RelayCommand]
    private void ExpandAll(string which) => SetExpanded(which, true);

    [RelayCommand]
    private void CollapseAll(string which) => SetExpanded(which, false);

    private void SetExpanded(string which, bool expanded)
    {
        var roots = which == "Raw" ? _rawRoots : _resourcesRoots;
        if (roots is null) return;

        var selected = _selectedNode;
        if (!expanded) SetSelectedNode(null);

        foreach (var root in roots)
            SetExpandedRecursive(root, expanded);

        if (expanded && selected is not null) SetSelectedNode(selected);
    }

    /// <summary>Finds, expands, and selects the first node matching the predicate in the Resources tree.</summary>
    /// <returns>True if a matching node was found and selected.</returns>
    public bool SelectResourcesNode(Func<TreeNode, bool> predicate)
        => SelectNode(_resourcesRoots, predicate) is not null;

    /// <summary>Shows the Raw tree tab with the named resource file selected in it.</summary>
    /// <returns>The node selected, or null when no tree node holds that file.</returns>
    public TreeNode? ShowRawResource(string resource)
    {
        // A filter left over from an earlier search would hide the file the link was followed to reach.
        RawFilterText = string.Empty;
        SelectedTreeTab = TreeTabs.Raw;
        return SelectNode(_rawRoots, n => n.Resource is { } name
            && name.Equals(resource, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Shows the given node selected in the tab it was reached from.</summary>
    public void Show(TreeNode node, int tab)
    {
        if (tab == TreeTabs.Raw) RawFilterText = string.Empty;
        else ResourcesFilterText = string.Empty;

        SelectedTreeTab = tab;
        SelectNode(tab == TreeTabs.Raw ? _rawRoots : _resourcesRoots, n => ReferenceEquals(n, node));
    }

    private TreeNode? SelectNode(List<TreeNode>? roots, Func<TreeNode, bool> predicate)
    {
        if (roots is null) return null;
        foreach (var root in roots)
        {
            if (SelectNodeRecursive(root, predicate) is { } found)
                return found;
        }
        return null;
    }

    private TreeNode? SelectNodeRecursive(TreeNode node, Func<TreeNode, bool> predicate)
    {
        if (predicate(node))
        {
            SetSelectedNode(node);
            return node;
        }

        foreach (var child in node.Children)
        {
            if (SelectNodeRecursive(child, predicate) is { } found)
            {
                node.IsExpanded = true;
                return found;
            }
        }

        return null;
    }

    /// <summary>Updates the selected node reference and selection state.</summary>
    public void SetSelectedNode(TreeNode? node)
    {
        if (_selectedNode is not null && !ReferenceEquals(_selectedNode, node))
            _selectedNode.IsSelected = false;

        _selectedNode = node;
        if (node is not null) node.IsSelected = true;
    }

    private static void SetExpandedRecursive(TreeNode node, bool expanded)
    {
        node.IsExpanded = expanded;
        foreach (var child in node.Children)
            SetExpandedRecursive(child, expanded);
    }
}
