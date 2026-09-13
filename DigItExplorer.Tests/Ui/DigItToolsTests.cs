using DigItExplorer.Core.Ui;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="DigItTools"/> lists every tool, in order, with this application marked.</summary>
public class DigItToolsTests
{
    /// <summary>The header reads as one sentence, whatever the link does to it in the markup.</summary>
    [Fact]
    public void The_header_still_reads_as_one_sentence_around_the_author()
    {
        Assert.Equal(
            "Free, unofficial tools for Dig It!, all made by Devilquest.",
            DigItTools.HeaderLead + DigItTools.Author + DigItTools.HeaderEnd);
    }

    [Fact]
    public void The_tools_are_listed_in_the_agreed_order()
    {
        Assert.Equal(
            ["Dig It! Explorer", "Dig It! Atlas", "Dig It! Patcher"],
            DigItTools.All.Select(tool => tool.Name));
    }

    [Fact]
    public void Every_tool_states_what_it_is_what_it_does_and_where_it_lives()
    {
        Assert.All(DigItTools.All, tool =>
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Name));
            Assert.False(string.IsNullOrWhiteSpace(tool.Kind));
            Assert.False(string.IsNullOrWhiteSpace(tool.Summary));
            Assert.True(tool.Link.IsAbsoluteUri);
        });
    }

    /// <summary>Each row reaches its own tool, so a placeholder standing in for a real address is a failure.</summary>
    [Fact]
    public void No_row_stands_on_the_author_profile_and_no_two_share_a_link()
    {
        Assert.All(DigItTools.All, tool => Assert.NotEqual(DigItTools.AuthorProfile, tool.Link));

        Assert.Equal(DigItTools.All.Count, DigItTools.All.Select(tool => tool.Link).Distinct().Count());
    }

    [Fact]
    public void Exactly_one_row_is_the_application_showing_the_window()
    {
        var here = Assert.Single(DigItTools.All, tool => tool.IsThisApp);

        Assert.Equal("Dig It! Explorer", here.Name);
    }

    /// <summary>The window is a signpost, so a summary says what a tool does and never how good it is.</summary>
    [Theory]
    [InlineData("best")]
    [InlineData("easy")]
    [InlineData("powerful")]
    [InlineData("simply")]
    [InlineData("ultimate")]
    public void No_summary_sells_anything(string word)
    {
        Assert.All(DigItTools.All, tool => Assert.DoesNotContain(word, tool.Summary, StringComparison.OrdinalIgnoreCase));
    }
}
