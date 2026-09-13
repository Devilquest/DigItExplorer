namespace DigItExplorer.Core.Ui;

/// <summary>Layer visibility carried between documents, where a layer follows the most recent thing said
/// about it or about a group holding it.</summary>
public sealed class StickyLayers
{
    private readonly Dictionary<string, Statement> _leaves = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Statement> _groups = new(StringComparer.Ordinal);
    private long _clock;

    /// <summary>Whether the structural layer is drawn when a document holding it opens.</summary>
    /// <param name="scope">Document kind the layer belongs to, keeping same-named layers of different kinds apart.</param>
    /// <param name="layer">The structural layer being asked about.</param>
    /// <param name="groupPath">Group names from the tree's root down to the layer's own group.</param>
    public bool IsVisible(string scope, MapLayer layer, IReadOnlyList<string> groupPath)
        => IsVisible(scope, LayerKey(scope, layer), groupPath, DefaultOf(layer));

    /// <summary>Whether a leaf is drawn, following whichever is newer of its own answer and the group
    /// answers above it, and its default while it has neither.</summary>
    /// <param name="scope">Document kind whose groups are consulted.</param>
    /// <param name="leaf">Key identifying the leaf, from one of the key builders.</param>
    /// <param name="groupPath">Group names from the tree's root down to the leaf's own group.</param>
    /// <param name="fallback">What the leaf shows while nothing has been said about it or about its groups.</param>
    public bool IsVisible(string scope, string leaf, IReadOnlyList<string> groupPath, bool fallback)
    {
        var own = _leaves.TryGetValue(leaf, out var said) ? said : (Statement?)null;
        var group = LatestIntent(scope, groupPath);
        if (own is { } o && group is { } g) return o.Said >= g.Said ? o.Visible : g.Visible;
        return own?.Visible ?? group?.Visible ?? fallback;
    }

    /// <summary>Records what the user said about one structural layer.</summary>
    /// <param name="scope">Document kind the layer belongs to.</param>
    /// <param name="layer">The structural layer being recorded.</param>
    /// <param name="visible">Whether the layer is drawn.</param>
    public void Record(string scope, MapLayer layer, bool visible) => Record(LayerKey(scope, layer), visible);

    /// <summary>Records what the user said about one leaf.</summary>
    /// <param name="leaf">Key identifying the leaf, from one of the key builders.</param>
    /// <param name="visible">Whether the leaf is drawn.</param>
    public void Record(string leaf, bool visible) => _leaves[leaf] = new Statement(visible, ++_clock);

    /// <summary>Records what the user said by pressing a group's own checkbox.</summary>
    /// <param name="scope">Document kind the group belongs to.</param>
    /// <param name="groupPath">Group names from the tree's root down to and including the pressed group.</param>
    /// <param name="visible">Whether the group's layers are drawn.</param>
    public void RecordIntent(string scope, IReadOnlyList<string> groupPath, bool visible)
    {
        if (groupPath.Count > 0) _groups[KeyOf(scope, groupPath)] = new Statement(visible, ++_clock);
    }

    /// <summary>The key one structural layer of one document kind is remembered under.</summary>
    /// <param name="scope">Document kind the layer belongs to.</param>
    /// <param name="layer">The structural layer being keyed.</param>
    public static string LayerKey(string scope, MapLayer layer) => $"{scope}/{layer}";

    /// <summary>The key one entity category or subtype is remembered under, which no document kind divides
    /// because the same creature is the same creature wherever it stands.</summary>
    /// <param name="category">Entity category byte.</param>
    /// <param name="type">Entity subtype, or null for a category with no breakdown.</param>
    public static string EntityKey(byte category, ushort? type) => $"entity/{category}:{type}";

    /// <summary>The key one mechanism's collision footprint is remembered under.</summary>
    /// <param name="category">Entity category byte.</param>
    /// <param name="type">Entity subtype, or null for a category with no breakdown.</param>
    public static string ColliderKey(byte category, ushort? type) => $"collider/{category}:{type}";

    /// <summary>The visibility a layer opens with while nothing has been said about it or about its groups.</summary>
    /// <param name="layer">The structural layer being asked about.</param>
    public static bool DefaultOf(MapLayer layer) => layer switch
    {
        // The overlays that read the game rather than draw it start off, and so do the signs of the one
        // group that shows a single member at a time.
        MapLayer.Collision or MapLayer.ExitInfo or MapLayer.BonusInfo or MapLayer.SpriteCollision
            or MapLayer.WorldMapPath or MapLayer.MainMenuSetup or MapLayer.MainMenuPlay
            or MapLayer.MainMenuIntro => false,
        _ => true,
    };

    /// <summary>The newest thing said about any group along the path, which is not always the innermost one:
    /// pressing an outer group in a document that does not show the inner one leaves the inner one older.</summary>
    private Statement? LatestIntent(string scope, IReadOnlyList<string> groupPath)
    {
        Statement? latest = null;
        for (int depth = groupPath.Count; depth > 0; depth--)
            if (_groups.TryGetValue(KeyOf(scope, groupPath, depth), out var said)
                && (latest is null || said.Said > latest.Value.Said))
                latest = said;
        return latest;
    }

    private static string KeyOf(string scope, IReadOnlyList<string> groupPath)
        => KeyOf(scope, groupPath, groupPath.Count);

    private static string KeyOf(string scope, IReadOnlyList<string> groupPath, int depth)
        => $"{scope}/{string.Join('/', groupPath.Take(depth))}";

    private readonly record struct Statement(bool Visible, long Said);
}
