namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>A labeled row in the Info panel, either half of which may name a resource file it leads to.</summary>
public readonly record struct InfoRow(string Label, string Value, string? LabelFile = null,
    string? ValueFile = null)
{
    /// <summary>Creates an InfoRow or returns null if the value is null or empty.</summary>
    public static InfoRow? OrNull(string label, string? value)
        => string.IsNullOrEmpty(value) ? null : new InfoRow(label, value);
}

/// <summary>A titled group of rows in the Info panel: Identity, Source, or Contents.</summary>
public sealed record InfoSection(string Title, IReadOnlyList<InfoRow> Rows);

/// <summary>Aggregates sections, standing knowledge notes, and damage warnings for a selected node.</summary>
/// <param name="Sections">The node's info rows grouped into sections.</param>
/// <param name="Note">Standing knowledge note about the node, or null.</param>
/// <param name="Damage">Damage or defect warning, completed by its reader with the repair tool, or null.</param>
public sealed record NodeInfo(IReadOnlyList<InfoSection> Sections, string? Note = null, string? Damage = null);

/// <summary>Builds an <see cref="InfoSection"/> from rows that may or may not apply to the node at hand.</summary>
internal static class InfoSections
{
    /// <summary>Builds an InfoSection from non-null rows, or returns null if empty.</summary>
    public static InfoSection? Of(string title, params InfoRow?[] rows)
    {
        var present = new List<InfoRow>(rows.Length);
        foreach (var row in rows)
            if (row is not null) present.Add(row.Value);
        return present.Count > 0 ? new InfoSection(title, present) : null;
    }
}

/// <summary>Builds a <see cref="NodeInfo"/> from sections that may or may not apply to the node at hand.</summary>
internal static class NodeInfos
{
    /// <summary>Drops every section <see cref="InfoSections.Of"/> came back empty for.</summary>
    public static NodeInfo Of(string? note, params InfoSection?[] sections)
    {
        var present = new List<InfoSection>(sections.Length);
        foreach (var section in sections)
            if (section is not null) present.Add(section);
        return new NodeInfo(present, note);
    }
}
