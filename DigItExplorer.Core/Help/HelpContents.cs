using System.Reflection;

namespace DigItExplorer.Core.Help;

/// <summary>Help topic metadata containing embedded markdown filename and display title.</summary>
public sealed record HelpTopic(string File, string Title);

/// <summary>Catalog and resource reader for embedded in-app help markdown documents.</summary>
public static class HelpContents
{
    /// <summary>Ordered list of all available help topics in the guide.</summary>
    public static IReadOnlyList<HelpTopic> Topics { get; } =
    [
        new("getting-started.md", "Getting Started"),
        new("the-window.md", "The Window"),
        new("raw-and-resources.md", "Resources and Raw"),
        new("the-layers-panel.md", "The Layers Panel"),
        new("the-info-panel.md", "The Info Panel"),
        new("viewing.md", "Viewing"),
        new("animations.md", "Animations"),
        new("audio.md", "Audio"),
        new("exporting.md", "Exporting"),
        new("when-something-is-wrong.md", "When Something Is Wrong"),
    ];

    /// <summary>Finds a help topic by markdown filename with case-insensitive matching.</summary>
    public static HelpTopic? Find(string? file)
        => Topics.FirstOrDefault(topic => string.Equals(topic.File, file, StringComparison.OrdinalIgnoreCase));

    /// <summary>Reads and parses the markdown document for a given help topic.</summary>
    public static HelpDocument Read(HelpTopic topic) => MarkdownReader.Read(TextOf(topic));

    /// <summary>Retrieves raw markdown text for a topic from assembly embedded resources.</summary>
    private static string TextOf(HelpTopic topic)
    {
        var assembly = typeof(HelpContents).Assembly;
        var name = Array.Find(assembly.GetManifestResourceNames(),
            candidate => candidate.EndsWith('.' + topic.File, StringComparison.Ordinal));

        if (name is null) return string.Empty;

        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null) return string.Empty;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
