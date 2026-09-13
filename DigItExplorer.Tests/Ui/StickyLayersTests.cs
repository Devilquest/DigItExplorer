using DigItExplorer.Core.Ui;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="StickyLayers"/>'s defaults, per-scope separation, and the rule that the
/// newest statement covering a leaf is the one it opens with.</summary>
public class StickyLayersTests
{
    private static readonly string[] Root = [];
    private static readonly string[] Base = ["Base"];
    private static readonly string[] BaseCollision = ["Base", "Collision"];
    private static readonly string[] Overlays = ["Info Overlays"];

    [Fact]
    public void A_layer_nobody_has_spoken_about_takes_its_default()
    {
        var layers = new StickyLayers();
        Assert.True(layers.IsVisible("Map", MapLayer.Terrain, Base));
        Assert.False(layers.IsVisible("Map", MapLayer.Collision, BaseCollision));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_recorded_choice_outranks_the_default(bool visible)
    {
        var layers = new StickyLayers();
        layers.Record("Map", MapLayer.Collision, visible);
        Assert.Equal(visible, layers.IsVisible("Map", MapLayer.Collision, BaseCollision));
    }

    [Fact]
    public void Turning_a_layer_off_is_remembered_rather_than_read_as_never_chosen()
    {
        var layers = new StickyLayers();
        layers.Record("Map", MapLayer.Terrain, false);
        Assert.False(layers.IsVisible("Map", MapLayer.Terrain, Base));
    }

    [Fact]
    public void The_same_layer_is_a_separate_choice_in_each_scope()
    {
        var layers = new StickyLayers();
        layers.Record("Map", MapLayer.Terrain, false);
        Assert.True(layers.IsVisible("MainMenu", MapLayer.Terrain, Base));
    }

    [Fact]
    public void Recording_a_layer_says_nothing_about_any_other()
    {
        var layers = new StickyLayers();
        layers.Record("Map", MapLayer.ExitInfo, true);
        Assert.False(layers.IsVisible("Map", MapLayer.BonusInfo, Overlays));
    }

    [Fact]
    public void A_layer_never_shown_follows_the_group_it_appears_under()
    {
        var layers = new StickyLayers();
        layers.RecordIntent("Map", Overlays, true);
        Assert.True(layers.IsVisible("Map", MapLayer.BonusInfo, Overlays));
    }

    [Fact]
    public void A_later_word_on_the_group_overrules_what_a_layer_was_left_at()
    {
        var layers = new StickyLayers();
        layers.Record("Map", MapLayer.BonusInfo, true);
        layers.RecordIntent("Map", Overlays, false);
        Assert.False(layers.IsVisible("Map", MapLayer.BonusInfo, Overlays));
    }

    [Fact]
    public void A_layer_touched_after_its_group_keeps_its_own_answer()
    {
        var layers = new StickyLayers();
        layers.RecordIntent("Map", Overlays, false);
        layers.Record("Map", MapLayer.BonusInfo, true);
        Assert.True(layers.IsVisible("Map", MapLayer.BonusInfo, Overlays));
    }

    [Fact]
    public void The_group_spoken_about_last_answers_even_when_it_is_the_outer_one()
    {
        var layers = new StickyLayers();
        layers.RecordIntent("Map", BaseCollision, true);
        layers.RecordIntent("Map", Base, false);
        Assert.False(layers.IsVisible("Map", MapLayer.Collision, BaseCollision));
    }

    [Fact]
    public void A_group_that_was_never_pressed_defers_to_one_further_out()
    {
        var layers = new StickyLayers();
        layers.RecordIntent("Map", Base, true);
        Assert.True(layers.IsVisible("Map", MapLayer.Collision, BaseCollision));
    }

    [Fact]
    public void A_group_choice_is_overwritten_rather_than_stacked()
    {
        var layers = new StickyLayers();
        layers.RecordIntent("Map", Base, true);
        layers.RecordIntent("Map", Base, false);
        Assert.False(layers.IsVisible("Map", MapLayer.Terrain, Base));
    }

    [Fact]
    public void A_group_of_the_same_name_is_a_separate_choice_in_each_scope()
    {
        var layers = new StickyLayers();
        layers.RecordIntent("Map", Base, false);
        Assert.True(layers.IsVisible("Slab", MapLayer.SlabText, Base));
    }

    [Fact]
    public void A_leaf_at_the_root_of_the_tree_has_no_group_to_inherit_from()
    {
        var layers = new StickyLayers();
        layers.RecordIntent("WorldMap", ["Signs"], true);
        Assert.False(layers.IsVisible("WorldMap", MapLayer.WorldMapPath, Root));
    }

    [Fact]
    public void A_footprint_never_shown_follows_the_collision_group()
    {
        var layers = new StickyLayers();
        // The footprint of a mechanism the user has already met somewhere, left at its default.
        layers.Record(StickyLayers.ColliderKey(0x04, 1), false);
        layers.RecordIntent("Map", BaseCollision, true);
        Assert.True(layers.IsVisible("Map", StickyLayers.ColliderKey(0x04, 0), BaseCollision, fallback: false));
        Assert.True(layers.IsVisible("Map", StickyLayers.ColliderKey(0x04, 1), BaseCollision, fallback: false));
    }

    [Fact]
    public void An_entity_is_the_same_entity_in_every_kind_of_document()
    {
        var layers = new StickyLayers();
        layers.Record(StickyLayers.EntityKey(0x13, 12), false);
        Assert.False(layers.IsVisible("MainMenu", StickyLayers.EntityKey(0x13, 12), Root, fallback: true));
    }

    [Fact]
    public void A_category_and_one_of_its_subtypes_are_told_apart()
    {
        var layers = new StickyLayers();
        layers.Record(StickyLayers.EntityKey(0x13, null), false);
        Assert.True(layers.IsVisible("Map", StickyLayers.EntityKey(0x13, 0), Root, fallback: true));
    }

    [Fact]
    public void Only_the_overlays_and_the_exclusive_signs_start_off()
    {
        MapLayer[] off =
        [
            MapLayer.Collision, MapLayer.ExitInfo, MapLayer.BonusInfo, MapLayer.SpriteCollision,
            MapLayer.WorldMapPath, MapLayer.MainMenuSetup, MapLayer.MainMenuPlay, MapLayer.MainMenuIntro,
        ];
        foreach (MapLayer layer in Enum.GetValues<MapLayer>())
            Assert.Equal(!off.Contains(layer), StickyLayers.DefaultOf(layer));
    }
}
