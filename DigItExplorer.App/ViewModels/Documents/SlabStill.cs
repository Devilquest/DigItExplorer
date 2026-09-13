using DigItExplorer.App.ViewModels.Layers;
using DigItExplorer.Core.Slabs;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed still document representing an Instructions or Credits slab resting stop.</summary>
internal sealed class SlabStill(PreviewContext context, int screen, int stopIndex, IReadOnlyList<string> infoPath)
    : ComposedStill(context)
{
    public override ZoomBucket Bucket => ZoomBucket.ScreensUi;

    protected override IReadOnlyList<string> InfoPath => infoPath;

    protected override ComposedFrame? Compose()
    {
        var result = SlabCompositor.Compose(Context.Library.TryRead, Context.Font!, Context.Data!.Slab, screen,
            stopIndex, SlabLayerForest.RenderOptionsFrom(Context.Settings));
        return result is null ? null : new ComposedFrame(result.Rgb, result.Width, result.Height, result.Alpha);
    }
}
