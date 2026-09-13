using DigItExplorer.App.ViewModels.Layers;
using DigItExplorer.Core.Ending;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed still document representing an end sequence credit screen.</summary>
internal sealed class EndSequenceStill(PreviewContext context, SheetImage sheet, int screenIndex,
    IReadOnlyList<string> infoPath) : ComposedStill(context)
{
    public override ZoomBucket Bucket => ZoomBucket.ScreensUi;

    protected override IReadOnlyList<string> InfoPath => infoPath;

    /// <summary>Subject identifier displayed on empty placeholder views.</summary>
    protected override string PlaceholderSubject => "ENDSEQ.MPF";

    protected override ComposedFrame? Compose()
    {
        var options = EndSequenceLayerForest.RenderOptionsFrom(Context.Settings);
        var result = EndSequenceCompositor.Compose(sheet, Context.Font!, screenIndex, Context.Data!.EndSequence, options);
        return result is null ? null : new ComposedFrame(result.Rgb, result.Width, result.Height, result.Alpha);
    }
}
