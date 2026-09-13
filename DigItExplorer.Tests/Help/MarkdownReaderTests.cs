using DigItExplorer.Core.Help;

namespace DigItExplorer.Tests.Help;

/// <summary>Verifies Markdown parsing, inline styling, list nesting, code blocks, tables, and unsupported feature detection in <see cref="MarkdownReader"/>.</summary>
public class MarkdownReaderTests
{
    private static IReadOnlyList<HelpBlock> BlocksOf(string source)
    {
        var document = MarkdownReader.Read(source);
        Assert.Empty(document.Unsupported);
        return document.Blocks;
    }

    private static T Single<T>(string source) where T : HelpBlock
        => Assert.IsType<T>(Assert.Single(BlocksOf(source)));

    /// <summary>The visible text of a run of inlines, with every style flattened away.</summary>
    private static string Flatten(IReadOnlyList<HelpInline> content)
        => string.Concat(content.Select(run => run switch
        {
            HelpText t => t.Text,
            HelpBold b => b.Text,
            HelpItalic i => i.Text,
            HelpCode c => c.Text,
            HelpLink l => l.Text,
            _ => string.Empty,
        }));

    // ---- Headings ----

    [Theory]
    [InlineData("# Getting started", 1)]
    [InlineData("## The window", 2)]
    [InlineData("### Tab selection", 3)]
    public void A_heading_keeps_its_level(string source, int level)
        => Assert.Equal(level, Single<HelpHeading>(source).Level);

    [Fact]
    public void A_heading_carries_the_anchor_a_link_can_aim_at()
    {
        var heading = Single<HelpHeading>("## Raw and Resources");
        Assert.Equal("Raw and Resources", heading.Text);
        Assert.Equal("raw-and-resources", heading.Anchor);
    }

    /// <summary>Verifies that unsupported heading levels (> 3) are preserved as literal text with diagnostics.</summary>
    [Fact]
    public void A_fourth_heading_level_is_reported_and_kept_as_prose()
    {
        var document = MarkdownReader.Read("#### Too deep");

        Assert.Contains("deeper than level 3", Assert.Single(document.Unsupported));
        Assert.Equal("#### Too deep", Flatten(Assert.IsType<HelpParagraph>(Assert.Single(document.Blocks)).Content));
    }

    /// <summary>Verifies that sentences beginning with a hash and number are treated as prose rather than headings.</summary>
    [Fact]
    public void A_hash_that_opens_a_sentence_is_not_a_broken_heading()
    {
        var paragraph = Single<HelpParagraph>("#1 in the list is the one to pick.");
        Assert.Equal("#1 in the list is the one to pick.", Flatten(paragraph.Content));
    }

    // ---- Paragraphs ----

    /// <summary>Verifies that line-wrapped prose in a paragraph normalizes to single continuous text.</summary>
    [Fact]
    public void A_wrapped_paragraph_comes_back_as_one()
    {
        var paragraph = Single<HelpParagraph>("""
            Point the application at the folder
            holding your own copy of the game.
            """);

        Assert.Equal("Point the application at the folder holding your own copy of the game.", Flatten(paragraph.Content));
    }

    [Fact]
    public void A_blank_line_separates_two_paragraphs()
        => Assert.Equal(2, BlocksOf("First.\n\nSecond.").Count);

    // ---- Inline runs ----

    [Fact]
    public void The_four_styled_runs_are_recognized()
    {
        var content = Single<HelpParagraph>("Open **Raw**, pick *any* `MAIN.EXE`, see [the guide](using.md).").Content;

        Assert.Equal("Raw", Assert.IsType<HelpBold>(content[1]).Text);
        Assert.Equal("any", Assert.IsType<HelpItalic>(content[3]).Text);
        Assert.Equal("MAIN.EXE", Assert.IsType<HelpCode>(content[5]).Text);

        var link = Assert.IsType<HelpLink>(content[7]);
        Assert.Equal("the guide", link.Text);
        Assert.Equal("using.md", link.Target);
    }

    [Fact]
    public void A_link_target_is_handed_over_unresolved()
    {
        var content = Single<HelpParagraph>("See [exporting](exporting.md#what-is-written) and [the site](https://example.org).").Content;

        Assert.Equal("exporting.md#what-is-written", content.OfType<HelpLink>().First().Target);
        Assert.Equal("https://example.org", content.OfType<HelpLink>().Last().Target);
    }

    /// <summary>Verifies that unmatched markdown delimiters in prose remain literal text.</summary>
    [Theory]
    [InlineData("A lone * asterisk stays.", "A lone * asterisk stays.")]
    [InlineData("An unclosed **bold never opens.", "An unclosed **bold never opens.")]
    [InlineData("A lone ` backtick stays.", "A lone ` backtick stays.")]
    [InlineData("Brackets [like these] are text.", "Brackets [like these] are text.")]
    [InlineData("2 * 3 * 4 is arithmetic.", "2 * 3 * 4 is arithmetic.")]
    public void An_unmatched_delimiter_reaches_the_page_as_itself(string source, string expected)
    {
        var document = MarkdownReader.Read(source);
        Assert.Empty(document.Unsupported);
        Assert.Equal(expected, Flatten(Assert.IsType<HelpParagraph>(Assert.Single(document.Blocks)).Content));
    }

    [Fact]
    public void A_backslash_writes_a_delimiter_literally()
    {
        var paragraph = Single<HelpParagraph>(@"Write \*stars\* and a \` backtick.");
        Assert.Equal("Write *stars* and a ` backtick.", Flatten(paragraph.Content));
    }

    /// <summary>Verifies that unsupported image tags are reported and emitted as raw text.</summary>
    [Fact]
    public void An_image_is_reported_and_written_out_as_it_stands()
    {
        var document = MarkdownReader.Read("Before ![the window](window.png) after.");

        Assert.Contains("![the window](window.png)", Assert.Single(document.Unsupported));

        var paragraph = Assert.IsType<HelpParagraph>(Assert.Single(document.Blocks));
        Assert.Empty(paragraph.Content.OfType<HelpLink>());
        Assert.Equal("Before ![the window](window.png) after.", Flatten(paragraph.Content));
    }

    // ---- Lists ----

    [Fact]
    public void A_bulleted_list_keeps_its_items_in_order()
    {
        var list = Single<HelpList>("- First\n- Second\n- Third");

        Assert.False(list.Ordered);
        Assert.Equal(["First", "Second", "Third"], list.Items.Select(item => Flatten(item.Content)));
    }

    [Fact]
    public void A_numbered_list_is_marked_as_one()
        => Assert.True(Single<HelpList>("1. First\n2. Second").Ordered);

    [Fact]
    public void A_wrapped_item_comes_back_as_one()
    {
        var list = Single<HelpList>("""
            - An item long enough that its author
              wrapped it onto a second line.
            - A short one.
            """);

        Assert.Equal("An item long enough that its author wrapped it onto a second line.", Flatten(list.Items[0].Content));
        Assert.Equal("A short one.", Flatten(list.Items[1].Content));
    }

    [Fact]
    public void One_level_of_nesting_hangs_off_the_item_above_it()
    {
        var list = Single<HelpList>("""
            - Parent
              - Child one
              - Child two
            - Sibling
            """);

        Assert.Equal(2, list.Items.Count);
        Assert.Equal("Parent", Flatten(list.Items[0].Content));

        var nested = Assert.IsType<HelpList>(list.Items[0].Nested);
        Assert.Equal(["Child one", "Child two"], nested.Items.Select(item => Flatten(item.Content)));

        Assert.Null(list.Items[1].Nested);
    }

    [Fact]
    public void A_second_level_of_nesting_is_reported_and_folded_into_the_item()
    {
        var document = MarkdownReader.Read("""
            - Parent
              - Child
                - Grandchild
            """);

        Assert.Single(document.Unsupported);
        Assert.Contains("nested more than one level", Assert.Single(document.Unsupported));

        var nested = Assert.IsType<HelpList>(Assert.IsType<HelpList>(Assert.Single(document.Blocks)).Items[0].Nested);
        Assert.Contains("Grandchild", Flatten(Assert.Single(nested.Items).Content));
    }

    [Fact]
    public void A_bulleted_list_under_a_numbered_one_is_its_own_block()
    {
        var blocks = BlocksOf("1. Numbered\n- Bulleted");

        Assert.True(Assert.IsType<HelpList>(blocks[0]).Ordered);
        Assert.False(Assert.IsType<HelpList>(blocks[1]).Ordered);
    }

    // ---- Code blocks ----

    [Fact]
    public void A_fenced_block_keeps_its_text_verbatim()
    {
        var block = Single<HelpCodeBlock>("""
            ```
            Menu bar
            |  tree  |  preview  |
            ```
            """);

        Assert.Equal("Menu bar\n|  tree  |  preview  |", block.Text);
    }

    [Fact]
    public void A_language_name_on_the_fence_is_dropped_with_it()
        => Assert.Equal("dotnet build", Single<HelpCodeBlock>("```sh\ndotnet build\n```").Text);

    /// <summary>Verifies that unterminated code fences emit a diagnostic without consuming subsequent headings.</summary>
    [Fact]
    public void An_unterminated_fence_does_not_swallow_the_rest_of_the_page()
    {
        var document = MarkdownReader.Read("""
            ```
            let loose = true

            ## Still a heading
            """);

        Assert.Contains("Unterminated code fence", Assert.Single(document.Unsupported));
        Assert.Equal("Still a heading", document.Blocks.OfType<HelpHeading>().Single().Text);
    }

    // ---- Quotes, tables, rules ----

    [Fact]
    public void A_wrapped_quote_comes_back_as_one()
    {
        var quote = Single<HelpQuote>("""
            > The game is not included, and
            > you supply your own copy.
            """);

        Assert.Equal("The game is not included, and you supply your own copy.", Flatten(quote.Content));
    }

    [Fact]
    public void A_nested_quote_is_reported_and_kept()
    {
        var document = MarkdownReader.Read("> > Twice quoted");

        Assert.Contains("Nested blockquote", Assert.Single(document.Unsupported));
        Assert.Equal("Twice quoted", Flatten(Assert.IsType<HelpQuote>(Assert.Single(document.Blocks)).Content));
    }

    [Fact]
    public void A_table_takes_its_header_from_the_row_above_the_dashes()
    {
        var table = Single<HelpTable>("""
            | Shape | What it writes |
            |---|---|
            | Still | One image |
            | Strip | Every frame |
            """);

        Assert.Equal(["Shape", "What it writes"], table.Header.Cells.Select(Flatten));
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(["Strip", "Every frame"], table.Rows[1].Cells.Select(Flatten));
    }

    /// <summary>Verifies that pipe characters in regular prose without header rows do not create tables.</summary>
    [Fact]
    public void A_paragraph_holding_a_pipe_is_not_a_table()
    {
        var paragraph = Single<HelpParagraph>("| is a character the game uses.");
        Assert.Equal("| is a character the game uses.", Flatten(paragraph.Content));
    }

    [Fact]
    public void Column_alignment_markers_are_reported()
    {
        var document = MarkdownReader.Read("| A | B |\n|:--|--:|\n| 1 | 2 |");

        Assert.Contains("alignment", Assert.Single(document.Unsupported));
        Assert.IsType<HelpTable>(Assert.Single(document.Blocks));
    }

    [Fact]
    public void A_rule_is_its_own_block()
        => Assert.IsType<HelpRule>(BlocksOf("Above.\n\n---\n\nBelow.")[1]);

    // ---- The two properties that hold everywhere ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n\n")]
    [InlineData("|")]
    [InlineData("|||")]
    [InlineData("#")]
    [InlineData("- ")]
    [InlineData("[](")]
    [InlineData("![](")]
    [InlineData("```")]
    [InlineData("> ")]
    [InlineData("1.")]
    [InlineData("\0�*`[")]
    public void Nothing_handed_to_the_reader_makes_it_throw(string? source)
        => Assert.NotNull(MarkdownReader.Read(source));

    /// <summary>Verifies that Unix (LF) and Windows (CRLF) line endings parse to identical document structures.</summary>
    [Fact]
    public void Both_line_endings_parse_the_same_way()
    {
        var unix = MarkdownReader.Read("# Title\n\nA paragraph.\n\n- An item");
        var windows = MarkdownReader.Read("# Title\r\n\r\nA paragraph.\r\n\r\n- An item");

        Assert.Equal(unix.Blocks.Count, windows.Blocks.Count);
        Assert.Equal("Title", Assert.IsType<HelpHeading>(windows.Blocks[0]).Text);
        Assert.Equal("A paragraph.", Flatten(Assert.IsType<HelpParagraph>(windows.Blocks[1]).Content));
        Assert.Equal("An item", Flatten(Assert.Single(Assert.IsType<HelpList>(windows.Blocks[2]).Items).Content));
    }

    /// <summary>Verifies that comprehensive markdown documents conforming to the supported subset produce no diagnostics.</summary>
    [Fact]
    public void A_page_using_the_whole_subset_reports_nothing_unsupported()
    {
        var document = MarkdownReader.Read("""
            # Getting started

            The application reads **your own copy** of the game. It is not included, and
            nothing here redistributes it.

            > Point it at the folder holding `MAIN.EXE`.

            ## What counts as a folder

            1. The folder holding the archives.
            2. A folder holding a `DIGIT` folder that holds them.

            ---

            ### If it is refused

            - Nothing already open is closed.
              - The tree stays as it was.
              - The preview stays as it was.
            - The message says [why](damaged.md#what-damaged-means).

            ```
            Menu bar
            | tree | preview | panel |
            ```

            | Shape | Writes |
            |---|---|
            | Still | One image |
            """);

        Assert.Empty(document.Unsupported);
        Assert.Contains(document.Blocks, b => b is HelpHeading);
        Assert.Contains(document.Blocks, b => b is HelpQuote);
        Assert.Contains(document.Blocks, b => b is HelpList { Ordered: true });
        Assert.Contains(document.Blocks, b => b is HelpRule);
        Assert.Contains(document.Blocks, b => b is HelpCodeBlock);
        Assert.Contains(document.Blocks, b => b is HelpTable);
    }
}
