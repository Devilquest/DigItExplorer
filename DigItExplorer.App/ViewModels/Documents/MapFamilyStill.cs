using DigItExplorer.App.Services;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Base class for map documents sharing captured 1:1 zoom framing behavior.</summary>
internal abstract class MapFamilyStill(PreviewContext context) : ComposedStill(context)
{
    private PreviewFraming _captured;

    public override ZoomBucket Bucket => ZoomBucket.Map;

    /// <summary>Captures current map 1:1 zoom state prior to layer changes.</summary>
    public void CaptureFraming() => _captured = PreviewFramingRule.CaptureOneToOne(Context.Settings.MapIsOneToOne);

    protected override PreviewFraming CurrentFraming() => Framing.FromCaptured(_captured);

    protected override void OnCleared() => CaptureFraming();
}
