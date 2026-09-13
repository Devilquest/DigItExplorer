using DigItExplorer.Core.Ui;

namespace DigItExplorer.Tests;

/// <summary>Verifies panel tab preference retention, fallback switching, and seeding in <see cref="PanelTabSelection"/>.</summary>
public class PanelTabSelectionTests
{
    [Fact]
    public void A_node_offering_both_tabs_shows_the_chosen_one()
    {
        var tabs = new PanelTabSelection();
        tabs.Offer(layers: true, info: true);

        Assert.Equal(PanelTab.Layers, tabs.Chosen);
        Assert.Equal(PanelTab.Layers, tabs.Displayed);
    }

    [Fact]
    public void Choosing_info_survives_a_node_that_offers_no_layers()
    {
        var tabs = new PanelTabSelection();
        tabs.Offer(layers: true, info: true);
        tabs.Choose(PanelTab.Info);

        tabs.Offer(layers: false, info: true);
        Assert.Equal(PanelTab.Info, tabs.Displayed);

        tabs.Offer(layers: true, info: true);
        Assert.Equal(PanelTab.Info, tabs.Displayed);
    }

    [Fact]
    public void Falling_back_to_info_does_not_overwrite_a_chosen_layers()
    {
        var tabs = new PanelTabSelection();
        tabs.Offer(layers: true, info: true);
        tabs.Choose(PanelTab.Layers);

        // What the control does on a node with no layers: the Layers tab goes away and the selection lands
        // on Info by itself. That is a fallback, not a pick.
        tabs.Offer(layers: false, info: true);
        tabs.Choose(tabs.Displayed);

        Assert.Equal(PanelTab.Layers, tabs.Chosen);

        tabs.Offer(layers: true, info: true);
        Assert.Equal(PanelTab.Layers, tabs.Displayed);
    }

    [Fact]
    public void A_node_that_offers_only_layers_shows_them_even_with_info_chosen()
    {
        var tabs = new PanelTabSelection();
        tabs.Offer(layers: true, info: true);
        tabs.Choose(PanelTab.Info);

        tabs.Offer(layers: true, info: false);

        Assert.Equal(PanelTab.Layers, tabs.Displayed);
        Assert.Equal(PanelTab.Info, tabs.Chosen);
    }

    [Fact]
    public void A_node_that_offers_neither_tab_leaves_the_preference_alone()
    {
        var tabs = new PanelTabSelection();
        tabs.Offer(layers: true, info: true);
        tabs.Choose(PanelTab.Info);

        tabs.Offer(layers: false, info: false);
        tabs.Choose(tabs.Displayed);

        Assert.Equal(PanelTab.Info, tabs.Chosen);
    }

    [Fact]
    public void A_seeded_tab_is_shown_immediately_when_both_are_on_offer()
    {
        // A restored session seeds the constructor with whatever tab was picked last.
        var tabs = new PanelTabSelection(PanelTab.Info);
        tabs.Offer(layers: true, info: true);

        Assert.Equal(PanelTab.Info, tabs.Chosen);
        Assert.Equal(PanelTab.Info, tabs.Displayed);
    }

    [Fact]
    public void Seeding_the_initial_tab_does_not_bypass_the_one_tab_offered_rule()
    {
        // Restored as Info, but the first node happens to offer only Layers: that is a fallback, not a
        // pick, and must not silently overwrite what a restored session was seeded with.
        var tabs = new PanelTabSelection(PanelTab.Info);
        tabs.Offer(layers: true, info: false);
        tabs.Choose(tabs.Displayed);

        Assert.Equal(PanelTab.Info, tabs.Chosen);

        tabs.Offer(layers: true, info: true);
        Assert.Equal(PanelTab.Info, tabs.Displayed);
    }
}
