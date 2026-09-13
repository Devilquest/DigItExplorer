using System.Text;

namespace DigItExplorer.Core.Help;

/// <summary>Generates URL/anchor slug identifiers from markdown headings.</summary>
internal static class HelpAnchor
{
    /// <summary>Generates a lowercased, hyphen-separated slug for a heading text.</summary>
    /// <param name="heading">Heading text string.</param>
    /// <returns>Sanitized anchor slug string.</returns>
    public static string For(string heading)
    {
        var slug = new StringBuilder(heading?.Length ?? 0);

        foreach (var c in heading ?? string.Empty)
        {
            if (char.IsLetterOrDigit(c))
            {
                slug.Append(char.ToLowerInvariant(c));
            }
            else if (c == ' ')
            {
                slug.Append('-');
            }
            else if (c == '-')
            {
                slug.Append(c);
            }
        }

        return slug.ToString();
    }
}
