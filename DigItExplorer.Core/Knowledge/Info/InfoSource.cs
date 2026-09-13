using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Generates Source section rows and sections for resource items in the Info panel.</summary>
internal static class InfoSource
{
    /// <summary>Generates info rows for specified resource names present in the library.</summary>
    public static IEnumerable<InfoRow> Rows(ResourceLibrary library, IEnumerable<string> names,
        bool leadsToTheFile = true)
        => names.Where(library.Contains)
                .Select(n => new InfoRow(n, $"{InfoFormat.Bytes(library.SizeOf(n))} · {library.ArchiveOf(n)}",
                    LabelFile: leadsToTheFile ? n : null));

    /// <summary>Enumerates all library resource names matching the specified prefix.</summary>
    public static IEnumerable<string> NamesStartingWith(ResourceLibrary library, string prefix)
        => library.Names.Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>Creates a Source section for the specified resource names, or null if none exist.</summary>
    public static InfoSection? Section(ResourceLibrary library, params string[] names)
        => Of(library, names);

    /// <inheritdoc cref="Section"/>
    public static InfoSection? Of(ResourceLibrary library, IEnumerable<string> names)
        => InfoSections.Of("Source", [.. Rows(library, names).Select(r => (InfoRow?)r)]);

    /// <summary>Creates a Source section naming the file without leading anywhere, for the node that is
    /// that file.</summary>
    public static InfoSection? SectionOfItself(ResourceLibrary library, string name)
        => InfoSections.Of("Source",
            [.. Rows(library, [name], leadsToTheFile: false).Select(r => (InfoRow?)r)]);
}
