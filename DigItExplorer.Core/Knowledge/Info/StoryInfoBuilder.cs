using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Story;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for scrolling story and lore scenes.</summary>
public static class StoryInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for a story scene.</summary>
    public static NodeInfo Build(string? worldName, string backgroundFile, string textFile, StoryScene scene,
        ResourceLibrary library)
    {
        var identity = InfoSections.Of("Identity", InfoRow.OrNull("World", worldName));

        var source = InfoSource.Section(library, backgroundFile, textFile);

        var contents = InfoSections.Of("Contents",
            new InfoRow("Background", $"{FrameCodec.Width}×{FrameCodec.Height} px"),
            new InfoRow("Text canvas", $"{FrameCodec.Width}×{scene.TextCanvasHeight} px"),
            new InfoRow("Scroll", $"{scene.TotalScroll} px over {scene.ScrollOffsets.Length} ticks"));

        return NodeInfos.Of(null, identity, source, contents);
    }
}
