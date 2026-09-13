namespace DigItExplorer.Core.Ui;

/// <summary>One tool of the Dig It! set, as the Dig It! Tools window lists it.</summary>
public sealed record DigItTool(string Name, string Kind, string Summary, Uri Link, bool IsThisApp);

/// <summary>The tools built for Dig It!, in the order the Dig It! Tools window shows them.</summary>
public static class DigItTools
{
    /// <summary>The author, credited the same way the About window credits them.</summary>
    public const string Author = "Devilquest";

    /// <summary>Where the author's name links to.</summary>
    public static Uri AuthorProfile { get; } = new("https://github.com/Devilquest");

    /// <summary>The header line up to the author's name, split out so the name itself can be the link.</summary>
    public const string HeaderLead = "Free, unofficial tools for Dig It!, all made by ";

    /// <summary>The header line after the author's name.</summary>
    public const string HeaderEnd = ".";

    /// <summary>What stands in place of a link on the row for the application already open.</summary>
    public const string HereMarker = "You are here";

    /// <summary>The tool that repairs a damaged copy of the game, named wherever damage is reported.</summary>
    public static DigItTool Patcher { get; } = new("Dig It! Patcher", "Windows application",
        "Repairs a damaged copy of Dig It! and fixes bugs in the original version.",
        new Uri("https://github.com/Devilquest/DigItPatcher"), IsThisApp: false);

    /// <summary>The tools, in display order, with this application marked.</summary>
    public static IReadOnlyList<DigItTool> All { get; } =
    [
        new("Dig It! Explorer", "Windows application",
            "Browses and reconstructs Dig It! levels, animations, screens, and audio from a local copy with "
            + "no emulator.",
            new Uri("https://github.com/Devilquest/DigItExplorer"), IsThisApp: true),
        new("Dig It! Atlas", "Website",
            "Maps every Dig It! level in the browser, with layers, entity positions, and interactive navigation.",
            new Uri("https://devilquest.github.io/DigItAtlas/"), IsThisApp: false),
        Patcher,
    ];
}
