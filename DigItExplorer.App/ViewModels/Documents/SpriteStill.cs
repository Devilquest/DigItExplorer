using DigItExplorer.Core.Maps;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Base class for composed stills whose whole frame is one sprite compositor's result.</summary>
internal abstract class SpriteStill(PreviewContext context, IReadOnlyList<string> infoPath)
    : ComposedStill(context)
{
    public override ZoomBucket Bucket => ZoomBucket.AnimationPlayer;

    protected override IReadOnlyList<string> InfoPath => infoPath;

    /// <summary>Default sticky zoom and fit settings from the animation player bucket.</summary>
    protected override (bool IsFitMode, double Zoom) StickyZoomSettings
        => (Context.Settings.AnimationIsFitMode, Context.Settings.AnimationZoom);

    /// <summary>Rendered sprite dimensions, or null until a compose succeeds.</summary>
    public (int Width, int Height)? CellSize { get; private set; }

    /// <summary>Runs this document's compositor, returning null if no layers are visible.</summary>
    protected abstract SpriteRenderResult? ComposeSprite();

    protected override ComposedFrame? Compose()
    {
        var result = ComposeSprite();
        if (result is null) return null;

        CellSize = (result.Width, result.Height);
        return new ComposedFrame(result.Rgb, result.Width, result.Height, result.Alpha);
    }
}
