namespace DigItExplorer.Core.Help;

/// <summary>Inline text run element within a markdown block.</summary>
public abstract record HelpInline;

/// <summary>Unformatted plain text inline run.</summary>
public sealed record HelpText(string Text) : HelpInline;

/// <summary>Bold formatted text inline run.</summary>
public sealed record HelpBold(string Text) : HelpInline;

/// <summary>Italic formatted text inline run.</summary>
public sealed record HelpItalic(string Text) : HelpInline;

/// <summary>Monospaced code formatted text inline run.</summary>
public sealed record HelpCode(string Text) : HelpInline;

/// <summary>Hyperlink inline run with display text and target destination.</summary>
public sealed record HelpLink(string Text, string Target) : HelpInline;
