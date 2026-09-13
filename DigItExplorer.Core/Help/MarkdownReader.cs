using System.Text;

namespace DigItExplorer.Core.Help;

/// <summary>Parses custom subset markdown text into structural HelpDocument block and inline elements.</summary>
internal sealed class MarkdownReader
{
    /// <summary>Set of markdown punctuation characters escapable with a backslash.</summary>
    private const string Escapable = "\\*`[]!";

    private readonly string[] _lines;
    private readonly List<string> _unsupported = [];
    private int _at;

    private MarkdownReader(string? text)
        => _lines = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    /// <summary>Parses a raw markdown string into a structured HelpDocument.</summary>
    public static HelpDocument Read(string? text) => new MarkdownReader(text).ReadDocument();

    private HelpDocument ReadDocument()
    {
        var blocks = new List<HelpBlock>();

        while (_at < _lines.Length)
        {
            if (ReadBlock() is { } block)
            {
                blocks.Add(block);
            }
        }

        return new HelpDocument(blocks, _unsupported);
    }

    /// <summary>Parses the next structural block starting at the current line position.</summary>
    private HelpBlock? ReadBlock()
    {
        var line = _lines[_at];

        if (line.Trim().Length == 0)
        {
            _at++;
            return null;
        }

        if (IsFence(line)) return ReadCodeBlock();
        if (IsRule(line)) { _at++; return new HelpRule(); }
        if (line.StartsWith('#')) return ReadHeading();
        if (IsQuote(line)) return ReadQuote();
        if (StartsTable()) return ReadTable();
        if (MarkerOf(line) is { } marker) return ReadList(marker.Ordered, marker.Indent);

        return ReadParagraph();
    }

    // ---- Blocks ----

    private HelpBlock ReadCodeBlock()
    {
        int fence = _at;
        int close = fence + 1;
        while (close < _lines.Length && !IsFence(_lines[close]))
        {
            close++;
        }

        if (close >= _lines.Length)
        {
            _unsupported.Add($"Unterminated code fence at line {fence + 1}.");
            _at = fence + 1;
            return new HelpParagraph([new HelpText(_lines[fence].Trim())]);
        }

        var body = string.Join("\n", _lines[(fence + 1)..close]);
        _at = close + 1;
        return new HelpCodeBlock(body);
    }

    private HelpBlock ReadHeading()
    {
        var line = _lines[_at];

        int level = 0;
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }

        bool spaced = level < line.Length && line[level] == ' ';
        if (level > 3 || !spaced)
        {
            if (level > 3 && spaced)
            {
                _unsupported.Add($"Heading deeper than level 3 at line {_at + 1}.");
            }
            return ReadParagraph();
        }

        var text = line[(level + 1)..].Trim();
        _at++;
        return new HelpHeading(level, text, HelpAnchor.For(text));
    }

    private HelpBlock ReadQuote()
    {
        var text = new StringBuilder();

        while (_at < _lines.Length && IsQuote(_lines[_at]))
        {
            var content = _lines[_at].TrimStart()[1..];
            if (content.StartsWith(' '))
            {
                content = content[1..];
            }

            if (content.TrimStart().StartsWith('>'))
            {
                _unsupported.Add($"Nested blockquote at line {_at + 1}.");
                content = content.TrimStart()[1..];
            }

            AppendWrapped(text, content.Trim());
            _at++;
        }

        return new HelpQuote(ParseInlines(text.ToString()));
    }

    private HelpBlock ReadTable()
    {
        var header = ReadRow(_lines[_at]);

        if (_lines[_at + 1].Contains(':'))
        {
            _unsupported.Add($"Table column alignment at line {_at + 2}.");
        }

        _at += 2;

        var rows = new List<HelpTableRow>();
        while (_at < _lines.Length && _lines[_at].TrimStart().StartsWith('|'))
        {
            rows.Add(ReadRow(_lines[_at]));
            _at++;
        }

        return new HelpTable(header, rows);
    }

    private HelpTableRow ReadRow(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|')) trimmed = trimmed[1..];
        if (trimmed.EndsWith('|')) trimmed = trimmed[..^1];

        var cells = new List<IReadOnlyList<HelpInline>>();
        foreach (var cell in trimmed.Split('|'))
        {
            cells.Add(ParseInlines(cell.Trim()));
        }

        return new HelpTableRow(cells);
    }

    /// <summary>Parses bulleted or numbered list items at a given indentation level.</summary>
    private HelpList ReadList(bool ordered, int indent, bool allowNesting = true)
    {
        var items = new List<HelpListItem>();
        var text = new StringBuilder();
        HelpList? nested = null;

        void Close()
        {
            if (text.Length > 0 || nested is not null)
            {
                items.Add(new HelpListItem(ParseInlines(text.ToString()), nested));
                text.Clear();
                nested = null;
            }
        }

        while (_at < _lines.Length)
        {
            var line = _lines[_at];
            if (line.Trim().Length == 0) break;

            if (MarkerOf(line) is { } marker)
            {
                if (marker.Indent < indent) break;

                if (marker.Indent == indent)
                {
                    if (marker.Ordered != ordered) break;

                    Close();
                    text.Append(line[marker.ContentAt..].Trim());
                    _at++;
                    continue;
                }

                if (allowNesting)
                {
                    nested = ReadList(marker.Ordered, marker.Indent, allowNesting: false);
                    continue;
                }

                _unsupported.Add($"List nested more than one level at line {_at + 1}.");
                AppendWrapped(text, line.Trim());
                _at++;
                continue;
            }

            if (StartsBlock(line)) break;

            AppendWrapped(text, line.Trim());
            _at++;
        }

        Close();
        return new HelpList(ordered, items);
    }

    private HelpBlock ReadParagraph()
    {
        var text = new StringBuilder(_lines[_at].Trim());
        _at++;

        while (_at < _lines.Length)
        {
            var line = _lines[_at];
            if (line.Trim().Length == 0 || StartsBlock(line) || MarkerOf(line) is not null) break;

            AppendWrapped(text, line.Trim());
            _at++;
        }

        return new HelpParagraph(ParseInlines(text.ToString()));
    }

    // ---- Inline runs ----

    private IReadOnlyList<HelpInline> ParseInlines(string text)
    {
        var runs = new List<HelpInline>();
        var plain = new StringBuilder();
        int i = 0;

        void ClosePlain()
        {
            if (plain.Length > 0)
            {
                runs.Add(new HelpText(plain.ToString()));
                plain.Clear();
            }
        }

        while (i < text.Length)
        {
            var c = text[i];

            if (c == '\\' && i + 1 < text.Length && Escapable.Contains(text[i + 1]))
            {
                plain.Append(text[i + 1]);
                i += 2;
                continue;
            }

            // A delimiter with no partner passes through as literal text.
            if (c == '`')
            {
                int close = text.IndexOf('`', i + 1);
                if (close > i + 1)
                {
                    ClosePlain();
                    runs.Add(new HelpCode(text[(i + 1)..close]));
                    i = close + 1;
                    continue;
                }
            }
            else if (c == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                int close = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (Emphasizes(text, i + 2, close))
                {
                    ClosePlain();
                    runs.Add(new HelpBold(text[(i + 2)..close]));
                    i = close + 2;
                    continue;
                }
            }
            else if (c == '*')
            {
                int close = text.IndexOf('*', i + 1);
                if (Emphasizes(text, i + 1, close))
                {
                    ClosePlain();
                    runs.Add(new HelpItalic(text[(i + 1)..close]));
                    i = close + 1;
                    continue;
                }
            }
            else if (c == '!' && i + 1 < text.Length && text[i + 1] == '[')
            {
                if (TryReadLink(text, i + 1, out var label, out var source, out int after))
                {
                    _unsupported.Add($"Image: ![{label}]({source}).");
                    plain.Append(text[i..after]);
                    i = after;
                    continue;
                }
            }
            else if (c == '[')
            {
                if (TryReadLink(text, i, out var label, out var target, out int after))
                {
                    ClosePlain();
                    runs.Add(new HelpLink(label, target));
                    i = after;
                    continue;
                }
            }

            plain.Append(c);
            i++;
        }

        ClosePlain();
        return runs;
    }

    /// <summary>Checks whether delimiters enclose valid non-whitespace text for emphasis.</summary>
    private static bool Emphasizes(string text, int from, int close)
        => close > from && !char.IsWhiteSpace(text[from]) && !char.IsWhiteSpace(text[close - 1]);

    private static bool TryReadLink(string text, int at, out string label, out string target, out int after)
    {
        label = string.Empty;
        target = string.Empty;
        after = at;

        int close = text.IndexOf(']', at + 1);
        if (close < 0 || close + 1 >= text.Length || text[close + 1] != '(') return false;

        int end = text.IndexOf(')', close + 2);
        if (end < 0) return false;

        label = text[(at + 1)..close];
        target = text[(close + 2)..end];
        after = end + 1;
        return true;
    }

    // ---- Line shapes ----

    /// <summary>List item marker metadata with indent level, ordering flag, and content offset.</summary>
    private readonly record struct ItemMarker(int Indent, bool Ordered, int ContentAt);

    private static ItemMarker? MarkerOf(string line)
    {
        int indent = 0;
        while (indent < line.Length && line[indent] == ' ')
        {
            indent++;
        }

        if (line[indent..].StartsWith("- ", StringComparison.Ordinal))
        {
            return new ItemMarker(indent, Ordered: false, indent + 2);
        }

        int digits = indent;
        while (digits < line.Length && char.IsAsciiDigit(line[digits]))
        {
            digits++;
        }

        if (digits > indent && line[digits..].StartsWith(". ", StringComparison.Ordinal))
        {
            return new ItemMarker(indent, Ordered: true, digits + 2);
        }

        return null;
    }

    private static bool IsFence(string line) => line.TrimStart().StartsWith("```", StringComparison.Ordinal);

    private static bool IsQuote(string line) => line.TrimStart().StartsWith('>');

    private static bool IsRule(string line)
    {
        var trimmed = line.Trim();
        return trimmed.Length >= 3 && trimmed.All(c => c == '-');
    }

    /// <summary>Checks whether a line starts a new structural markdown block element.</summary>
    private static bool StartsBlock(string line)
        => IsFence(line) || IsRule(line) || line.StartsWith('#') || IsQuote(line)
           || line.TrimStart().StartsWith('|');

    private bool StartsTable()
        => _lines[_at].TrimStart().StartsWith('|')
           && _at + 1 < _lines.Length
           && IsTableSeparator(_lines[_at + 1]);

    /// <summary>Checks whether a line is a valid table header-body delimiter row.</summary>
    private static bool IsTableSeparator(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith('|')) return false;

        bool dashed = false;
        foreach (var c in trimmed)
        {
            if (c == '-') dashed = true;
            else if (c is not ('|' or ' ' or ':')) return false;
        }

        return dashed;
    }

    /// <summary>Appends a continuation line of prose to a text buffer separated by a single space.</summary>
    private static void AppendWrapped(StringBuilder buffer, string piece)
    {
        if (piece.Length == 0) return;
        if (buffer.Length > 0) buffer.Append(' ');
        buffer.Append(piece);
    }
}
