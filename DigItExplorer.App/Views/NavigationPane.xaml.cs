using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DigItExplorer.App.Models;

namespace DigItExplorer.App.Views;

/// <summary>Navigation sidebar managing Raw and Resources tree views and search filters.</summary>
internal partial class NavigationPane : UserControl
{
    /// <summary>Layout passes a reveal waits through before giving up on the row appearing.</summary>
    private const int RevealPasses = 8;

    private bool _scrollRequested;
    private bool _restoringSelection;
    private RequestBringIntoViewEventArgs? _judgedRequest;

    /// <summary>Occurs when a resource or preview leaf node is selected in the tree.</summary>
    internal event EventHandler<TreeNode>? ResourceSelected;

    public NavigationPane()
    {
        InitializeComponent();
        KeepScrollAcrossSearches(ResourcesTree, ResourceFilterBox);
        KeepScrollAcrossSearches(RawTree, FilterBox);
    }

    /// <summary>Restores scroll offset when search query is cleared.</summary>
    private static void KeepScrollAcrossSearches(TreeView tree, TextBox filterBox)
    {
        const int MaxPasses = 8;

        ScrollViewer? viewer = null;
        double resting = 0;
        bool searching = false;

        tree.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((_, e) =>
        {
            viewer ??= e.OriginalSource as ScrollViewer;
            if (!searching) resting = viewer?.VerticalOffset ?? 0;
        }));

        void ScrollBackTo(double target, int passesLeft)
        {
            viewer?.ScrollToVerticalOffset(target);

            tree.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                bool arrived = viewer is null || Math.Abs(viewer.VerticalOffset - target) < 0.5;
                if (!arrived && passesLeft > 0)
                {
                    ScrollBackTo(target, passesLeft - 1);
                    return;
                }
                searching = false;
            });
        }

        filterBox.TextChanged += (_, _) =>
        {
            if (filterBox.Text.Trim().Length > 0)
            {
                searching = true;
                return;
            }
            if (searching) ScrollBackTo(resting, MaxPasses);
        };
    }

    /// <summary>Scrolls a tree to the specified node, resting it in the middle of the view.</summary>
    internal void Reveal(TreeNode node, int tab)
    {
        var tree = tab == TreeTabs.Raw ? RawTree : ResourcesTree;
        AfterLayout(() => Reveal(tree, node, RevealPasses));
    }

    private void Reveal(TreeView tree, TreeNode node, int passesLeft)
    {
        tree.UpdateLayout();

        // A tab switched to a moment ago has no height yet, and a region half of nothing tall centers
        // nothing.
        if (tree.ActualHeight <= 0 || ContainerOf(tree, node) is not { } container)
        {
            if (passesLeft > 0) AfterLayout(() => Reveal(tree, node, passesLeft - 1));
            return;
        }

        // Asking for a region one viewport tall centered on the row, rather than for the row itself, is
        // what leaves it in the middle: bringing the row alone into view stops as soon as it is inside the
        // edge, which is where a jump from the info panel would leave it sitting on the last visible line.
        _scrollRequested = true;
        container.BringIntoView(new Rect(0, (container.ActualHeight - tree.ActualHeight) / 2,
            container.ActualWidth, tree.ActualHeight));
    }

    private void AfterLayout(Action action) => Dispatcher.BeginInvoke(DispatcherPriority.Loaded, action);

    /// <summary>Finds the row for a node, realizing every branch on the way down to it.</summary>
    private static TreeViewItem? ContainerOf(ItemsControl parent, TreeNode target)
    {
        foreach (var item in parent.Items)
        {
            if (item is not TreeNode node) continue;

            if (ReferenceEquals(node, target)) return RowFor(parent, node);
            if (!Holds(node, target)) continue;

            if (RowFor(parent, node) is not { } branch) return null;
            branch.UpdateLayout();
            return ContainerOf(branch, target);
        }
        return null;
    }

    private static TreeViewItem? RowFor(ItemsControl parent, TreeNode node)
    {
        int index = parent.Items.IndexOf(node);
        if (index < 0) return null;
        if (parent.ItemContainerGenerator.ContainerFromIndex(index) is TreeViewItem realized) return realized;

        // A row the panel has never scrolled past has no container of its own until the panel is asked
        // for that one index.
        if (PanelOf(parent) is { } panel)
        {
            panel.BringIndexIntoViewPublic(index);
            parent.UpdateLayout();
        }
        return parent.ItemContainerGenerator.ContainerFromIndex(index) as TreeViewItem;
    }

    private static VirtualizingPanel? PanelOf(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is VirtualizingPanel panel) return panel;
            if (PanelOf(child) is { } found) return found;
        }
        return null;
    }

    private static bool Holds(TreeNode node, TreeNode target)
    {
        foreach (var child in node.Children)
            if (ReferenceEquals(child, target) || Holds(child, target)) return true;
        return false;
    }

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not TreeNode node) return;

        if (!node.IsSelectable)
        {
            node.IsSelected = false;
            if (e.OldValue is TreeNode leaf)
            {
                _restoringSelection = true;
                leaf.IsSelected = true;
                _restoringSelection = false;
            }
            return;
        }
        ResourceSelected?.Invoke(this, node);
    }

    private void TreeViewItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem tvi && tvi.DataContext is TreeNode node)
        {
            if (!node.IsSelectable)
            {
                tvi.IsExpanded = !tvi.IsExpanded;
                e.Handled = true;
            }
        }
    }

    private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
    {
        if (_restoringSelection) return;

        if (ReferenceEquals(e.OriginalSource, sender) && sender is TreeViewItem tvi)
        {
            _scrollRequested = true;
            tvi.BringIntoView();
        }
    }

    private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
        // Only the row that raised the request decides. The event bubbles up through every ancestor row,
        // each carrying this same handler, so without this the row itself would spend the one permission
        // and the row above it would then cancel the request outright, leaving every scroll to a nested
        // row dead before it reached the ScrollViewer.
        if (ReferenceEquals(e, _judgedRequest)) return;
        _judgedRequest = e;

        if (_scrollRequested) _scrollRequested = false;
        else e.Handled = true;
    }

}
