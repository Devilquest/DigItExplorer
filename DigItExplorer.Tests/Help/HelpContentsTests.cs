using System.Reflection;
using DigItExplorer.Core.Help;

namespace DigItExplorer.Tests.Help;

/// <summary>Verifies embedded help topic manifest integrity, link resolution, and Markdown subset conformance in <see cref="HelpContents"/>.</summary>
public class HelpContentsTests
{
    private static readonly Assembly Core = typeof(HelpContents).Assembly;

    private static IEnumerable<string> EmbeddedPages()
        => Core.GetManifestResourceNames().Where(name => name.EndsWith(".md", StringComparison.Ordinal));

    // ---- The manifest and the files agree ----

    [Theory]
    [InlineData("viewing.md")]
    [InlineData("Viewing.md")]
    [InlineData("VIEWING.MD")]
    public void A_topic_is_found_however_its_link_was_typed(string written)
        => Assert.Equal("Viewing", HelpContents.Find(written)?.Title);

    [Fact]
    public void A_name_no_page_carries_is_found_by_nothing()
    {
        Assert.Null(HelpContents.Find("no-such-page.md"));
        Assert.Null(HelpContents.Find(null));
    }

    [Fact]
    public void Every_topic_has_a_page_behind_it()
    {
        foreach (var topic in HelpContents.Topics)
        {
            Assert.NotEmpty(HelpContents.Read(topic).Blocks);
        }
    }

    /// <summary>Guards that every embedded markdown help resource corresponds to a registered topic.</summary>
    [Fact]
    public void Every_embedded_page_is_listed_in_the_contents()
    {
        // Counted as well as walked. The loop below passes on an empty sequence, so without this the whole
        // test would go green on a build that embedded nothing at all.
        Assert.Equal(HelpContents.Topics.Count, EmbeddedPages().Count());

        foreach (var resource in EmbeddedPages())
        {
            var listed = HelpContents.Topics.Any(topic => resource.EndsWith('.' + topic.File, StringComparison.Ordinal));
            Assert.True(listed, $"{resource} is embedded but is not a topic in HelpContents.");
        }
    }

    [Fact]
    public void The_contents_lists_each_page_once()
    {
        Assert.Equal(HelpContents.Topics.Count, HelpContents.Topics.Select(t => t.File).Distinct().Count());
        Assert.Equal(HelpContents.Topics.Count, HelpContents.Topics.Select(t => t.Title).Distinct().Count());
    }

    // ---- Every page holds to the subset ----

    /// <summary>Guards that no shipped help topic requires unsupported Markdown fallback formatting.</summary>
    [Fact]
    public void No_page_reaches_the_literal_fallback()
    {
        foreach (var topic in HelpContents.Topics)
        {
            var unsupported = HelpContents.Read(topic).Unsupported;
            Assert.True(unsupported.Count == 0, $"{topic.File}: {string.Join(" ", unsupported)}");
        }
    }

    [Fact]
    public void Every_page_opens_with_one_title_and_has_no_second()
    {
        foreach (var topic in HelpContents.Topics)
        {
            var blocks = HelpContents.Read(topic).Blocks;

            var title = Assert.IsType<HelpHeading>(blocks[0]);
            Assert.Equal(1, title.Level);
            Assert.Single(blocks.OfType<HelpHeading>(), heading => heading.Level == 1);
        }
    }

    /// <summary>Guards that heading anchors within any help topic remain distinct.</summary>
    [Fact]
    public void No_page_has_two_headings_with_the_same_anchor()
    {
        foreach (var topic in HelpContents.Topics)
        {
            var anchors = HelpContents.Read(topic).Blocks.OfType<HelpHeading>().Select(h => h.Anchor).ToList();
            Assert.Equal(anchors.Count, anchors.Distinct().Count());
        }
    }

    // ---- Every link arrives somewhere ----

    /// <summary>Guards that all internal help topic links and heading anchors resolve successfully.</summary>
    [Fact]
    public void Every_internal_link_resolves()
    {
        var anchors = HelpContents.Topics.ToDictionary(
            topic => topic.File,
            topic => HelpContents.Read(topic).Blocks.OfType<HelpHeading>().Select(h => h.Anchor).ToHashSet(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var topic in HelpContents.Topics)
        {
            foreach (var link in LinksIn(HelpContents.Read(topic).Blocks))
            {
                if (link.Target.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;

                var parts = link.Target.Split('#', 2);
                var file = parts[0].Length == 0 ? topic.File : parts[0];

                var destination = HelpContents.Find(file);
                Assert.True(destination is not null, $"{topic.File}: link to {link.Target} names no page.");

                if (parts.Length == 2)
                {
                    Assert.True(anchors[destination.File].Contains(parts[1]),
                        $"{topic.File}: link to {link.Target} names no heading in {destination.File}.");
                }
            }
        }
    }

    [Fact]
    public void Every_external_link_is_secure()
    {
        foreach (var topic in HelpContents.Topics)
        {
            foreach (var link in LinksIn(HelpContents.Read(topic).Blocks))
            {
                Assert.False(link.Target.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
                    $"{topic.File}: {link.Target} is not https.");
            }
        }
    }

    private static IEnumerable<HelpLink> LinksIn(IEnumerable<HelpBlock> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case HelpParagraph paragraph:
                    foreach (var link in paragraph.Content.OfType<HelpLink>()) yield return link;
                    break;

                case HelpQuote quote:
                    foreach (var link in quote.Content.OfType<HelpLink>()) yield return link;
                    break;

                case HelpList list:
                    foreach (var link in LinksInList(list)) yield return link;
                    break;

                case HelpTable table:
                    foreach (var row in table.Rows.Prepend(table.Header))
                    {
                        foreach (var cell in row.Cells)
                        {
                            foreach (var link in cell.OfType<HelpLink>()) yield return link;
                        }
                    }
                    break;
            }
        }
    }

    private static IEnumerable<HelpLink> LinksInList(HelpList list)
    {
        foreach (var item in list.Items)
        {
            foreach (var link in item.Content.OfType<HelpLink>()) yield return link;

            if (item.Nested is not null)
            {
                foreach (var link in LinksInList(item.Nested)) yield return link;
            }
        }
    }
}
