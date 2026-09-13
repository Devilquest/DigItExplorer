using System.IO;

namespace DigItExplorer.Core.Export;

/// <summary>Defines file export eligibility and resource type filtering policies.</summary>
public static class ExportPolicy
{
    private static readonly HashSet<string> AudioExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".SMP", ".DAT" };

    /// <summary>Whether <paramref name="name"/> is one of the game's audio resources: a digitized sample
    /// or a music bank.</summary>
    public static bool IsAudio(string name) => AudioExtensions.Contains(Path.GetExtension(name));

    /// <summary>Whether <paramref name="name"/> may be written out as a file at all.</summary>
    public static bool MayExport(string name) => !IsAudio(name);
}
