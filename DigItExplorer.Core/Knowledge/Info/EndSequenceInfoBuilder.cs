using System.Text;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for ending sequence credits screens.</summary>
public static class EndSequenceInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for an ending credits screen.</summary>
    public static NodeInfo Build(int screenIndex, EndSequenceData data, ResourceLibrary library)
    {
        var captions = screenIndex >= 0 && screenIndex < data.Frames.Count ? data.Frames[screenIndex] : [];

        var identity = InfoSections.Of("Identity",
            new InfoRow("Screen", $"{screenIndex + 1} of {data.Frames.Count}"));

        var source = InfoSource.Section(library, data.FileName);

        var tune = GameKnowledge.TuneFile(data.TuneName);
        var contentRows = new List<InfoRow?>
        {
            new InfoRow("Frame", screenIndex.ToString()),
            new InfoRow("Music", tune, ValueFile: library.Contains(tune) ? tune : null),
        };
        contentRows.AddRange(captions.Select((c, i) =>
            (InfoRow?)new InfoRow($"Text {i + 1}", Encoding.Latin1.GetString(c.Text))));
        var contents = InfoSections.Of("Contents", [.. contentRows]);

        return NodeInfos.Of(null, identity, source, contents);
    }
}
