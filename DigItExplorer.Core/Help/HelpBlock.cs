namespace DigItExplorer.Core.Help;

/// <summary>Structural block element of a parsed help document.</summary>
public abstract record HelpBlock;

/// <summary>Heading block element with level, text, and slug anchor.</summary>
public sealed record HelpHeading(int Level, string Text, string Anchor) : HelpBlock;

/// <summary>Paragraph block element containing inline text runs.</summary>
public sealed record HelpParagraph(IReadOnlyList<HelpInline> Content) : HelpBlock;

/// <summary>List item element containing inline content and optional nested sub-list.</summary>
public sealed record HelpListItem(IReadOnlyList<HelpInline> Content, HelpList? Nested);

/// <summary>Ordered or unordered list block element.</summary>
public sealed record HelpList(bool Ordered, IReadOnlyList<HelpListItem> Items) : HelpBlock;

/// <summary>Fenced code block element containing raw verbatim text.</summary>
public sealed record HelpCodeBlock(string Text) : HelpBlock;

/// <summary>Blockquote callout element containing inline runs.</summary>
public sealed record HelpQuote(IReadOnlyList<HelpInline> Content) : HelpBlock;

/// <summary>Table row element containing a list of cell contents.</summary>
public sealed record HelpTableRow(IReadOnlyList<IReadOnlyList<HelpInline>> Cells);

/// <summary>Table block element containing a header row and body rows.</summary>
public sealed record HelpTable(HelpTableRow Header, IReadOnlyList<HelpTableRow> Rows) : HelpBlock;

/// <summary>Horizontal divider rule block element.</summary>
public sealed record HelpRule : HelpBlock;

/// <summary>Parsed help document containing block elements and any unsupported markup notices.</summary>
public sealed record HelpDocument(IReadOnlyList<HelpBlock> Blocks, IReadOnlyList<string> Unsupported);
