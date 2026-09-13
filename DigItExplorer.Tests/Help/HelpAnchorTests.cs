using DigItExplorer.Core.Help;

namespace DigItExplorer.Tests.Help;

/// <summary>Verifies heading slug generation and anchor link normalization in <see cref="HelpAnchor"/>.</summary>
public class HelpAnchorTests
{
    [Theory]
    [InlineData("The window", "the-window")]
    [InlineData("Raw and Resources", "raw-and-resources")]
    [InlineData("3.3 The declared subset", "33-the-declared-subset")]
    [InlineData("Markdown stays; the parser becomes ours", "markdown-stays-the-parser-becomes-ours")]
    [InlineData("What `Export…` writes", "what-export-writes")]
    [InlineData("Well-formed hyphens survive", "well-formed-hyphens-survive")]
    public void A_heading_slugs_the_way_the_web_page_slugs_it(string heading, string expected)
        => Assert.Equal(expected, HelpAnchor.For(heading));

    /// <summary>Verifies that consecutive spaces slug to individual hyphens matching web anchors.</summary>
    [Fact]
    public void Runs_of_spaces_are_not_collapsed()
        => Assert.Equal("two--spaces", HelpAnchor.For("Two  spaces"));

    [Fact]
    public void Nothing_slugs_to_nothing()
    {
        Assert.Equal(string.Empty, HelpAnchor.For(string.Empty));
        Assert.Equal(string.Empty, HelpAnchor.For("···"));
    }
}
