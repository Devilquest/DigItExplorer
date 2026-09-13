using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using DigItExplorer.App.Help;
using DigItExplorer.Core.Help;

namespace DigItExplorer.App.ViewModels;

/// <summary>Hierarchical contents tree entry representing a help topic page or section heading.</summary>
internal sealed partial class HelpEntry : ObservableObject
{
    public HelpEntry(HelpTopic topic, string title, string? anchor, IReadOnlyList<HelpEntry> children)
    {
        Topic = topic;
        Title = title;
        Anchor = anchor;
        Children = children;
    }

    public HelpTopic Topic { get; }

    public string Title { get; }

    /// <summary>Heading anchor identifier, or null for page roots.</summary>
    public string? Anchor { get; }

    public IReadOnlyList<HelpEntry> Children { get; }

    /// <summary>Whether child heading entries are currently expanded in the contents tree.</summary>
    [ObservableProperty] private bool _isExpanded;

    /// <summary>Whether this entry is currently selected in the contents tree.</summary>
    [ObservableProperty] private bool _isSelected;
}

/// <summary>ViewModel managing user guide topic parsing, contents navigation, and flow documents, parsing
/// every page once on first open because the contents cannot be built without reading all of them.</summary>
internal sealed partial class HelpViewModel : ObservableObject
{
    private readonly Dictionary<string, HelpDocument> _parsed = [];
    private readonly Dictionary<string, FlowDocument> _rendered = [];

    public HelpViewModel()
    {
        foreach (var topic in HelpContents.Topics)
        {
            _parsed[topic.File] = HelpContents.Read(topic);
        }

        Contents = HelpContents.Topics.Select(EntryFor).ToList();
    }

    /// <summary>Hierarchical table of contents for user guide topics.</summary>
    public IReadOnlyList<HelpEntry> Contents { get; }

    /// <summary>Flow document currently displayed in the help viewer.</summary>
    [ObservableProperty] private FlowDocument? _document;

    /// <summary>Help topic currently displayed.</summary>
    public HelpTopic? CurrentTopic { get; private set; }

    /// <summary>Displays the specified help topic document and updates contents selection.</summary>
    public bool Show(HelpTopic topic)
    {
        foreach (var entry in Contents)
        {
            entry.IsExpanded = entry.Topic == topic;
        }

        if (CurrentTopic == topic) return false;

        if (!_rendered.TryGetValue(topic.File, out var document))
        {
            document = HelpDocumentBuilder.Build(_parsed[topic.File]);
            _rendered[topic.File] = document;
        }

        CurrentTopic = topic;
        Document = document;
        return true;
    }

    /// <summary>Initial guide page entry displayed on launch.</summary>
    public HelpEntry Opening => Contents[0];

    /// <summary>Builds a contents entry hierarchy for the specified topic.</summary>
    private HelpEntry EntryFor(HelpTopic topic)
    {
        var children = _parsed[topic.File].Blocks
            .OfType<HelpHeading>()
            .Where(heading => heading.Level == 2)
            .Select(heading => new HelpEntry(topic, heading.Text, heading.Anchor, []))
            .ToList();

        return new HelpEntry(topic, topic.Title, null, children);
    }
}
