namespace DigItExplorer.App.Services;

/// <summary>Formats hierarchical breadcrumb text for the preview status bar.</summary>
internal static class InfoLine
{
    /// <summary>Separator string between breadcrumb components.</summary>
    private const string Separator = "    ·    ";

    /// <summary>Separator between breadcrumb components where the column is narrow.</summary>
    private const string TightSeparator = " · ";

    /// <summary>Joins non-empty breadcrumb fields, suppressing consecutive duplicate values.</summary>
    public static string Of(params string?[] fields) => Join(Separator, fields);

    /// <inheritdoc cref="Of"/>
    public static string Tight(params string?[] fields) => Join(TightSeparator, fields);

    private static string Join(string separator, string?[] fields)
    {
        var kept = new List<string>(fields.Length);
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field)) continue;
            if (kept.Count > 0 && string.Equals(kept[^1], field, StringComparison.OrdinalIgnoreCase)) continue;
            kept.Add(field);
        }
        return string.Join(separator, kept);
    }
}
