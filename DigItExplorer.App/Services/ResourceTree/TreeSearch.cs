using DigItExplorer.App.Models;

namespace DigItExplorer.App.Services.ResourceTree;

/// <summary>Filters resource tree node visibility based on search queries without rebuilding tree structure.</summary>
internal static class TreeSearch
{
    // Matches against the built tree, never against the files behind it: rebuilding from a filtered set
    // turns a level whose substages were all filtered but one into a selectable leaf, because the rule that
    // collapses a single-substage level cannot tell that case from a level that only ever had one.

    /// <summary>Applies a search filter to the specified tree roots, updating visibility and match counts.</summary>
    public static void Apply(IReadOnlyList<TreeNode> roots, string query)
    {
        if (query.Length == 0)
        {
            foreach (var root in roots) Reset(root);
        }
        else
        {
            foreach (var root in roots) Mark(root, query, ancestorMatched: false);
        }

        foreach (var root in roots) Refresh(root);
    }

    private static bool Mark(TreeNode node, string query, bool ancestorMatched)
    {
        bool matched = ancestorMatched || Matches(node, query);
        int shown = 0;

        foreach (var child in node.Children)
            if (Mark(child, query, matched))
                shown++;

        node.IsShown = matched || shown > 0;
        node.SetShownCount(shown);

        if (node.IsShown && node.Children.Count > 0)
        {
            // Runs again on every keystroke, so the first value captured is the one restored.
            node.ExpandedBeforeSearch ??= node.IsExpanded;
            node.IsExpanded = true;
        }

        return node.IsShown;
    }

    private static void Reset(TreeNode node)
    {
        node.IsShown = true;
        node.SetShownCount(null);

        if (node.ExpandedBeforeSearch is { } wasExpanded)
        {
            node.IsExpanded = wasExpanded;
            node.ExpandedBeforeSearch = null;
        }

        foreach (var child in node.Children) Reset(child);
    }

    private static void Refresh(TreeNode node)
    {
        node.RefreshVisibleChildren();
        foreach (var child in node.Children) Refresh(child);
    }

    private static bool Matches(TreeNode node, string query)
        => Contains(node.Label, query) || Contains(node.Resource, query) || Contains(node.LevelStem, query);

    private static bool Contains(string? text, string query)
        => text is not null && text.Contains(query, StringComparison.OrdinalIgnoreCase);
}
