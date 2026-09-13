using DigItExplorer.Core.Knowledge.Info;

namespace DigItExplorer.Tests;

/// <summary>Tests row pruning and empty section suppression in <see cref="InfoSections"/>.</summary>
public class NodeInfoTests
{
    [Fact]
    public void A_section_built_from_only_null_rows_is_itself_null()
    {
        var section = InfoSections.Of("Empty", InfoRow.OrNull("A", null), InfoRow.OrNull("B", ""));

        Assert.Null(section);
    }

    [Fact]
    public void A_section_with_at_least_one_row_keeps_only_the_rows_that_applied()
    {
        var section = InfoSections.Of("Mixed", InfoRow.OrNull("A", null), new InfoRow("B", "value"));

        Assert.NotNull(section);
        Assert.Single(section!.Rows);
        Assert.Equal("B", section.Rows[0].Label);
    }

    [Fact]
    public void A_row_leads_nowhere_unless_it_is_handed_a_file()
    {
        var row = new InfoRow("Label", "value");

        Assert.Null(row.LabelFile);
        Assert.Null(row.ValueFile);
    }

    [Fact]
    public void A_node_info_drops_every_section_that_came_back_empty()
    {
        var empty = InfoSections.Of("Empty", InfoRow.OrNull("A", null));
        var full = InfoSections.Of("Full", new InfoRow("A", "value"));

        var info = NodeInfos.Of(null, empty, full);

        Assert.Single(info.Sections);
        Assert.Equal("Full", info.Sections[0].Title);
    }
}
