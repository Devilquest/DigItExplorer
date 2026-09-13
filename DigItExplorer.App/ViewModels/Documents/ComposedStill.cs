using DigItExplorer.App.Converters;
using DigItExplorer.App.Services;
using DigItExplorer.App.Views;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed RGB frame buffer and dimensions with optional alpha channel.</summary>
internal readonly record struct ComposedFrame(byte[] Rgb, int Width, int Height, byte[]? Alpha = null);

/// <summary>Shared collaborators and state required for composed still rendering.</summary>
internal sealed record PreviewContext(
    PreviewStage Stage, SessionSettings Settings, ResourceLibrary Library, GameFont? Font, GameData? Data,
    LayersViewModel Layers, InfoViewModel Info);

/// <summary>Sticky viewport zoom categorization for composed document types.</summary>
internal enum ZoomBucket
{
    /// <summary>Screens and UI family sticky zoom bucket (320x200 full-frame images).</summary>
    ScreensUi,

    /// <summary>Animation player and moving platform sticky zoom bucket.</summary>
    AnimationPlayer,

    /// <summary>Map and level sticky 1:1 pixel zoom bucket.</summary>
    Map,
}

/// <summary>Base class managing loading, rendering, framing, and info readouts for composed still documents.</summary>
internal abstract class ComposedStill(PreviewContext context)
{
    /// <summary>Viewport framing state and transition rules.</summary>
    protected readonly PreviewFramingRule Framing = new();

    /// <summary>Shared preview collaborators.</summary>
    protected PreviewContext Context { get; } = context;

    /// <summary>Sticky zoom category bucket for this document kind.</summary>
    public abstract ZoomBucket Bucket { get; }

    /// <summary>Hierarchical breadcrumb path representing this document in the resource tree.</summary>
    protected abstract IReadOnlyList<string> InfoPath { get; }

    /// <summary>Composes frame pixel data from active layer settings, or returns null if no layers are visible.</summary>
    protected abstract ComposedFrame? Compose();

    /// <summary>Document subject name displayed on empty placeholder views.</summary>
    protected virtual string PlaceholderSubject => InfoLine.Of([.. InfoPath]);

    /// <summary>Default sticky zoom and fit settings for this document kind.</summary>
    protected virtual (bool IsFitMode, double Zoom) StickyZoomSettings
        => (Context.Settings.ScreensIsFitMode, Context.Settings.ScreensZoom);

    /// <summary>Resolves viewport framing parameters for a successful render.</summary>
    protected virtual PreviewFraming CurrentFraming()
    {
        var (isFitMode, zoom) = StickyZoomSettings;
        return Framing.FromStickyZoom(isFitMode, zoom);
    }

    /// <summary>Invoked after empty layer state clears the viewport.</summary>
    protected virtual void OnCleared() { }

    /// <summary>Arms the viewport to reframe on the next successful render.</summary>
    public virtual void Reframe() => Framing.Reframe();

    /// <summary>Renders the composed document according to active layer settings.</summary>
    public virtual bool Render()
    {
        var frame = Compose();
        if (frame is null)
        {
            Context.Stage.ShowPlaceholder($"{PlaceholderSubject}: no layers visible");
            _shownSize = null;
            Framing.Reframe();
            OnCleared();
            return false;
        }

        var framing = CurrentFraming();
        var (rgb, w, h, alpha) = frame.Value;
        var bitmap = alpha is null
            ? BitmapConverter.ToBitmapRgb24(rgb, w, h)
            : BitmapConverter.ToBitmapRgba(rgb, alpha, w, h);
        Context.Stage.ShowBitmap(bitmap, w, h, framing.Fit, framing.Zoom);
        Context.Stage.ShowBlankBackdrop();
        _shownSize = (w, h);
        RefreshInfoText();
        Framing.Framed();
        return true;
    }

    private (int Width, int Height)? _shownSize;

    /// <summary>Updates preview stage info text with current visible layer names.</summary>
    public void RefreshInfoText()
    {
        if (_shownSize is null) return;
        Context.Stage.SetInfoText(InfoLine.Of([.. InfoPath,
            string.Join(" + ", Context.Layers.VisibleLayerNames())]));
    }
}
