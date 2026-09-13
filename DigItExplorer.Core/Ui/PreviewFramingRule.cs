namespace DigItExplorer.Core.Ui;

/// <summary>Framing parameters for viewport stage rendering including fit mode and zoom.</summary>
/// <param name="Fit">Whether to fit image to viewport bounds.</param>
/// <param name="Zoom">Explicit zoom scale factor, or null to keep current stage zoom.</param>
public readonly record struct PreviewFraming(bool Fit, double? Zoom)
{
    /// <summary>Framing configuration preserving current viewport zoom and pan.</summary>
    public static PreviewFraming KeepCurrent => new(Fit: false, Zoom: null);
}

/// <summary>Manages initial framing and sticky viewport zoom state across document renders.</summary>
public sealed class PreviewFramingRule
{
    /// <summary>Whether the next render pass should reframe the stage.</summary>
    public bool NeedsFraming { get; private set; } = true;

    /// <summary>Flags the stage to be reframed on the next render pass.</summary>
    public void Reframe() => NeedsFraming = true;

    /// <summary>Records that initial stage framing has completed.</summary>
    public void Framed() => NeedsFraming = false;

    /// <summary>Calculates framing from live fit mode and zoom settings.</summary>
    public PreviewFraming FromStickyZoom(bool isFitMode, double zoom)
        => NeedsFraming ? new PreviewFraming(isFitMode, zoom) : PreviewFraming.KeepCurrent;

    /// <summary>Calculates framing from pre-captured framing parameters.</summary>
    public PreviewFraming FromCaptured(PreviewFraming captured)
        => NeedsFraming ? captured : PreviewFraming.KeepCurrent;

    /// <summary>Captures framing for document switches supporting 1:1 pixel scaling.</summary>
    public static PreviewFraming CaptureOneToOne(bool oneToOne)
        => new(Fit: !oneToOne, Zoom: oneToOne ? 1.0 : null);
}
