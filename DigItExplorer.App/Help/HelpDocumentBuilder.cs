using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DigItExplorer.Core.Help;

namespace DigItExplorer.App.Help;

/// <summary>Converts parsed help topics into WPF FlowDocument visual trees.</summary>
internal static class HelpDocumentBuilder
{
    // Every color is attached with SetResourceReference rather than assigned: the brush is looked up again
    // whenever its key is replaced, and a resolved brush would opt the whole guide out of the theme.

    private const double BodySize = 13;
    private const double CodeSize = 12;
    private const double BodyLineHeight = 20;

    /// <summary>Maximum layout column width in pixels.</summary>
    private const double PageWidth = 1920;

    /// <summary>Default monospaced font family for code blocks.</summary>
    private static readonly FontFamily Monospace = new("Consolas");

    /// <summary>Builds a FlowDocument representation of the specified help page.</summary>
    public static FlowDocument Build(HelpDocument page)
    {
        var document = new FlowDocument
        {
            FontSize = BodySize,
            LineHeight = BodyLineHeight,
            ColumnWidth = PageWidth,
            MaxPageWidth = PageWidth,
            PagePadding = new Thickness(28, 20, 28, 28),
            Background = Brushes.Transparent,
        };

        document.SetResourceReference(TextElement.ForegroundProperty, "PrimaryTextBrush");

        foreach (var block in page.Blocks)
        {
            document.Blocks.Add(BlockFor(block));
        }

        return document;
    }

    private static Block BlockFor(HelpBlock block) => block switch
    {
        HelpHeading heading => Heading(heading),
        HelpParagraph paragraph => Body(paragraph.Content),
        HelpList list => ListFor(list),
        HelpCodeBlock code => CodeBlock(code),
        HelpQuote quote => Quote(quote),
        HelpTable table => TableFor(table),
        HelpRule => Rule(),
        _ => new Paragraph(),
    };

    // ---- Blocks ----

    /// <summary>Builds heading block with level-appropriate typography.</summary>
    private static Block Heading(HelpHeading heading)
    {
        var (size, margin) = heading.Level switch
        {
            1 => (22.0, new Thickness(0, 0, 0, 14)),
            2 => (17.0, new Thickness(0, 24, 0, 8)),
            _ => (14.0, new Thickness(0, 18, 0, 6)),
        };

        var paragraph = new Paragraph(new Run(heading.Text))
        {
            FontSize = size,
            FontWeight = FontWeights.SemiBold,
            LineHeight = size * 1.35,
            Margin = margin,
            Tag = heading.Anchor,
        };

        paragraph.SetResourceReference(TextElement.ForegroundProperty, "EmphasisBrush");
        return paragraph;
    }

    /// <summary>Finds heading block matching anchor identifier.</summary>
    public static Block? HeadingAt(FlowDocument document, string anchor)
        => document.Blocks.FirstOrDefault(block => Equals(block.Tag, anchor));

    private static Paragraph Body(IReadOnlyList<HelpInline> content)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 12) };
        Fill(paragraph.Inlines, content);
        return paragraph;
    }

    private static Block ListFor(HelpList list)
    {
        var block = new List
        {
            MarkerStyle = list.Ordered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
            Margin = new Thickness(0, 0, 0, 12),
            MarkerOffset = 6,
            Padding = new Thickness(22, 0, 0, 0),
        };

        foreach (var item in list.Items)
        {
            var entry = new ListItem(Body(item.Content));
            entry.Blocks.FirstBlock.Margin = new Thickness(0, 0, 0, 4);

            if (item.Nested is not null)
            {
                entry.Blocks.Add(ListFor(item.Nested));
            }

            block.ListItems.Add(entry);
        }

        return block;
    }

    private static Block CodeBlock(HelpCodeBlock code)
    {
        var paragraph = new Paragraph
        {
            FontFamily = Monospace,
            FontSize = CodeSize,
            LineHeight = CodeSize * 1.4,
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 14),
            BorderThickness = new Thickness(1),
        };

        paragraph.SetResourceReference(TextElement.BackgroundProperty, "CodeBackgroundBrush");
        paragraph.SetResourceReference(Block.BorderBrushProperty, "BorderBrush");

        var lines = code.Text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run(lines[i]));
        }

        return paragraph;
    }

    private static Block Quote(HelpQuote quote)
    {
        var body = Body(quote.Content);
        body.Margin = new Thickness(0);

        var section = new Section(body)
        {
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(14, 6, 0, 6),
            Margin = new Thickness(0, 0, 0, 14),
        };

        section.SetResourceReference(Block.BorderBrushProperty, "QuoteBarBrush");
        return section;
    }

    /// <summary>Builds table block with horizontal divider borders.</summary>
    private static Block TableFor(HelpTable table)
    {
        var block = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 14) };

        int columns = table.Rows
            .Select(row => row.Cells.Count)
            .Append(table.Header.Cells.Count)
            .Max();

        for (int i = 0; i < columns; i++)
        {
            block.Columns.Add(new TableColumn());
        }

        var group = new TableRowGroup();
        block.RowGroups.Add(group);
        group.Rows.Add(Row(table.Header, header: true));

        foreach (var row in table.Rows)
        {
            group.Rows.Add(Row(row, header: false));
        }

        return block;
    }

    private static TableRow Row(HelpTableRow row, bool header)
    {
        var built = new TableRow();

        foreach (var cell in row.Cells)
        {
            var content = Body(cell);
            content.Margin = new Thickness(0);

            var box = new TableCell(content)
            {
                Padding = new Thickness(10, 7, 10, 7),
                BorderThickness = new Thickness(0, 0, 0, 1),
                FontWeight = header ? FontWeights.SemiBold : FontWeights.Normal,
            };

            box.SetResourceReference(TableCell.BorderBrushProperty, "BorderBrush");
            if (header) box.SetResourceReference(TextElement.BackgroundProperty, "PanelBrush");

            built.Cells.Add(box);
        }

        return built;
    }

    /// <summary>Builds horizontal divider rule block.</summary>
    private static Block Rule()
    {
        var line = new Border { Height = 1, Margin = new Thickness(0, 10, 0, 22) };
        line.SetResourceReference(Border.BackgroundProperty, "BorderBrush");
        return new BlockUIContainer(line);
    }

    // ---- Inline runs ----

    private static void Fill(InlineCollection target, IReadOnlyList<HelpInline> content)
    {
        foreach (var run in content)
        {
            target.Add(InlineFor(run));
        }
    }

    private static Inline InlineFor(HelpInline run)
    {
        switch (run)
        {
            case HelpBold bold:
                return new Run(bold.Text) { FontWeight = FontWeights.SemiBold };

            case HelpItalic italic:
                return new Run(italic.Text) { FontStyle = FontStyles.Italic };

            case HelpCode code:
                return CodeInline(code);

            case HelpLink link:
                return Link(link);

            default:
                return new Run(((HelpText)run).Text);
        }
    }

    /// <summary>Builds inline code badge element with a rounded border and monospaced typography,
    /// matching the exact 20px box dimensions (1px border, 18px content, 1px border) of Keyboard Shortcuts.</summary>
    private static Inline CodeInline(HelpCode code)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(7, 2, 7, 2),
            Margin = new Thickness(2, 0, 2, 0),
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
        };

        border.SetResourceReference(Border.BackgroundProperty, "CodeBackgroundBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

        var text = new TextBlock
        {
            Text = code.Text,
            FontFamily = Monospace,
            FontSize = CodeSize,
            LineHeight = 14,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
        };

        text.SetResourceReference(TextBlock.ForegroundProperty, "EmphasisBrush");

        border.Child = text;
        return new InlineUIContainer(border)
        {
            BaselineAlignment = BaselineAlignment.Center,
        };
    }

    /// <summary>Builds styled hyperlink inline element whose Tag carries the unresolved target for the
    /// hosting viewer to split into a page and a heading.</summary>
    private static Inline Link(HelpLink link)
    {
        var hyperlink = new Hyperlink(new Run(link.Text)) { Tag = link.Target };

        if (Uri.TryCreate(link.Target, UriKind.RelativeOrAbsolute, out var uri))
        {
            hyperlink.NavigateUri = uri;
        }

        hyperlink.SetResourceReference(FrameworkContentElement.StyleProperty, "AppHyperlinkStyle");
        return hyperlink;
    }
}
