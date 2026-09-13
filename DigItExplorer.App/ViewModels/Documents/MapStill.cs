using DigItExplorer.Core.Maps;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed still document representing level terrain, collision overlays, and entity layouts.</summary>
internal sealed class MapStill(PreviewContext context, MapDocument document, IReadOnlyList<string> infoPath)
    : MapFamilyStill(context)
{
    /// <summary>Document subject name displayed on empty placeholder views.</summary>
    protected override string PlaceholderSubject => document.Stem;

    protected override IReadOnlyList<string> InfoPath => infoPath;

    protected override ComposedFrame? Compose()
    {
        var options = Context.Layers.BuildRenderOptions();
        var result = MapRenderer.Render(document, options, Context.Library.TryRead,
            name => Context.Library.Names.Contains(name, StringComparer.OrdinalIgnoreCase), Context.Font);
        return result is null ? null : new ComposedFrame(result.Rgb, result.Width, result.Height, result.Alpha);
    }
}
