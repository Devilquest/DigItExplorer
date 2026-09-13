using DigItExplorer.Core.Audio;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for audio nodes.</summary>
public static class AudioInfoBuilder
{
    /// <summary>Constructs Source and Contents info sections for a synthesized LDS music track.</summary>
    public static NodeInfo BuildMusic(string name, ResourceLibrary library, LdsSynthesizer? synth,
        bool sourceLeadsToItself = false)
    {
        var source = sourceLeadsToItself
            ? InfoSource.SectionOfItself(library, name)
            : InfoSource.Section(library, name);

        if (synth is null)
            return NodeInfos.Of(NodeNotes.For(NoteKey.File(name)), source);

        var contents = InfoSections.Of("Contents",
            new InfoRow("Format", "OPL2 FM synthesis"),
            new InfoRow("Sample rate", InfoFormat.SampleRate(synth.SampleRate)),
            new InfoRow("Duration", InfoFormat.Duration(synth.SinglePassDuration.TotalSeconds)),
            new InfoRow("Mode", synth.Loops ? "Loop" : "Once"));

        return NodeInfos.Of(NodeNotes.For(NoteKey.File(name)), source, contents);
    }

    /// <summary>Constructs Source and Contents info sections for an uncompressed SMP sound effect.</summary>
    public static NodeInfo BuildSound(string name, ResourceLibrary library, int byteLength,
        bool sourceLeadsToItself = false)
    {
        var source = sourceLeadsToItself
            ? InfoSource.SectionOfItself(library, name)
            : InfoSource.Section(library, name);

        var contents = InfoSections.Of("Contents",
            new InfoRow("Format", "8-bit signed mono PCM"),
            new InfoRow("Sample rate", InfoFormat.SampleRate(SmpAudio.SampleRate)),
            new InfoRow("Duration", InfoFormat.Duration(SmpAudio.DurationOf(byteLength).TotalSeconds)),
            new InfoRow("Mode", "Once"));

        return NodeInfos.Of(NodeNotes.For(NoteKey.File(name)), source, contents);
    }
}
