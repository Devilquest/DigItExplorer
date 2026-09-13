using DigItExplorer.Core.Menu;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed still document representing the main menu screen (LVL900).</summary>
internal sealed class MainMenuStill(PreviewContext context, MainMenuDocument document, IReadOnlyList<string> infoPath)
    : ComposedStill(context)
{
    public override ZoomBucket Bucket => ZoomBucket.ScreensUi;

    protected override IReadOnlyList<string> InfoPath => infoPath;

    protected override ComposedFrame? Compose()
    {
        var result = MainMenuCompositor.Compose(document, Context.Layers.BuildMainMenuRenderOptions(),
            Context.Library.TryRead, name => Context.Library.Names.Contains(name, StringComparer.OrdinalIgnoreCase));
        return result is null ? null : new ComposedFrame(result.Rgb, result.Width, result.Height, result.Alpha);
    }
}
