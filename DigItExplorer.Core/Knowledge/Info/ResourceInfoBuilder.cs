using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for raw resource files.</summary>
public static class ResourceInfoBuilder
{
    /// <summary>Constructs Source and damage info sections for a raw resource file.</summary>
    public static NodeInfo Build(string name, ResourceLibrary library, SheetDefect defect = SheetDefect.None,
        bool sourceLeadsToItself = false)
        => NodeInfos.Of(NodeNotes.For(NoteKey.File(name)),
                sourceLeadsToItself ? InfoSource.SectionOfItself(library, name)
                    : InfoSource.Section(library, name))
            with { Damage = DamageNote.For(defect) };

    /// <summary>Constructs Source, Contents, and damage info sections for a raw graphic sheet file.</summary>
    public static NodeInfo BuildSheet(string name, ResourceLibrary library, int frameCount,
        SheetDefect defect = SheetDefect.None, bool sourceLeadsToItself = false)
    {
        var source = sourceLeadsToItself
            ? InfoSource.SectionOfItself(library, name)
            : InfoSource.Section(library, name);

        var contents = InfoSections.Of("Contents",
            new InfoRow("Dimensions", $"{FrameCodec.Width}×{FrameCodec.Height} px"),
            new InfoRow("Frames", frameCount.ToString()));

        return NodeInfos.Of(NodeNotes.For(NoteKey.File(name)), source, contents)
            with { Damage = DamageNote.For(defect) };
    }
}
