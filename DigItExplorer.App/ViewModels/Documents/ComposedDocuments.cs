namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Maintains the currently active composed still document instance.</summary>
internal sealed class ComposedDocuments
{
    private ComposedStill? _current;

    /// <summary>Whether a composed still document is currently loaded.</summary>
    public bool HasCurrent => _current is not null;

    /// <summary>Sticky zoom category bucket for the current document, or null.</summary>
    public ZoomBucket? CurrentBucket => _current?.Bucket;

    /// <summary>Installs and reframes the specified composed still document.</summary>
    public void Load(ComposedStill still)
    {
        _current = still;
        _current.Reframe();
    }

    /// <summary>Clears the current composed document reference.</summary>
    public void Clear() => _current = null;

    /// <summary>Renders the active document and returns whether a frame was displayed.</summary>
    public bool Render() => _current?.Render() ?? false;
}
