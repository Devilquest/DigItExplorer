using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Tests;

/// <summary>Guards that animation tables link character names dynamically to the executable's species roster.</summary>
public class CharacterRosterLabelTests
{
    /// <summary>Sets carrying a DLF category without a species roster ordinal (Nirpling, Fireball).</summary>
    private static readonly string[] NotInRoster = ["Nirpling", "Fireball (unused)"];

    private static SkinCatalog OpenCatalog()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));
        return new SkinCatalog(_ => null, data.Nodes, data.EntityNames);
    }

    [Fact]
    public void Every_species_set_but_the_documented_two_names_itself_from_the_roster()
    {
        var withoutOrdinal = AnimationTables.All.Values
            .Where(s => s.Category is not null && s.RosterOrdinal is null)
            .Select(s => s.Name)
            .OrderBy(n => n, StringComparer.Ordinal);

        Assert.Equal(NotInRoster.OrderBy(n => n, StringComparer.Ordinal), withoutOrdinal);
    }

    [Fact]
    public void No_two_sets_claim_the_same_roster_entry()
    {
        var ordinals = AnimationTables.All.Values
            .Where(s => s.RosterOrdinal is not null)
            .Select(s => s.RosterOrdinal!.Value)
            .ToList();

        Assert.Equal(ordinals.Count, ordinals.Distinct().Count());
    }

    /// <summary>Guards that every roster ordinal resolves to a primary species name rather than a subtitle.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_roster_ordinal_lands_on_a_name_this_install_actually_ships()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        foreach (var set in AnimationTables.All.Values.Where(s => s.RosterOrdinal is not null))
        {
            var name = data.EntityNames[set.RosterOrdinal!.Value];
            Assert.False(string.IsNullOrWhiteSpace(name), $"{set.Name}: roster entry is empty");
            Assert.False(name!.StartsWith('('), $"{set.Name}: roster entry is a subtitle, not a name");
        }
    }

    /// <summary>Verifies that all player animation sets are labeled "Dug" from their display names.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_player_set_is_labeled_from_its_display_name_not_the_roster()
    {
        var catalog = OpenCatalog();

        var playerSets = AnimationTables.All.Values
            .Where(s => s.Name.StartsWith("Dug", StringComparison.Ordinal) && s.Name != "DugDirt").ToList();

        Assert.Equal(8, playerSets.Count);
        Assert.All(playerSets, s =>
        {
            Assert.Null(s.RosterOrdinal);
            Assert.Equal("Dug", catalog.CharacterLabel(s));
        });

        Assert.Equal("Dug Dirt", catalog.CharacterLabel(AnimationTables.DugDirt));
    }

    /// <summary>Verifies that unnamed entities like Nirpling fall back to their animation table keys.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void The_hatchling_is_labeled_from_the_table_key_alone()
    {
        var catalog = OpenCatalog();

        var nirpling = AnimationTables.All["Nirpling"];
        Assert.Null(nirpling.RosterOrdinal);
        Assert.Null(nirpling.DisplayName);
        Assert.Equal("Nirpling", catalog.CharacterLabel(nirpling));
    }

    /// <summary>Verifies fallback to table key names when executable roster data is unavailable.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Characters_fall_back_to_their_table_key_when_the_roster_is_unreadable()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));
        var catalog = new SkinCatalog(_ => null, data.Nodes, names: null);

        Assert.Equal("Aqua Slugger", catalog.CharacterLabel(AnimationTables.All["Aqua Slugger"]));
        Assert.Equal("Dug", catalog.CharacterLabel(AnimationTables.All["Dug"]));
    }

    /// <summary>Guards naming consistency between EntityCategories and SkinCatalog across species categories.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.SupportedBuild)]
    [InlineData((byte)0x06)] // Slugger
    [InlineData((byte)0x07)] // Draggo
    [InlineData((byte)0x08)] // Rocker
    [InlineData((byte)0x0A)] // Nirp
    [InlineData((byte)0x0C)] // Spurk
    [InlineData((byte)0x0D)] // Hopper
    [InlineData((byte)0x0E)] // Papa Spurk
    [InlineData((byte)0x0F)] // Troggi
    [InlineData((byte)0x10)] // Grock
    [InlineData((byte)0x32)] // the Boss
    public void The_layers_panel_and_the_resources_tree_name_a_species_alike(byte category)
    {
        var catalog = OpenCatalog();
        Assert.True(TestPaths.TryGetGameData(out var data));

        var set = Assert.Single(AnimationTables.All.Values,
            s => s.Category == category && s.RosterOrdinal is not null);

        Assert.Equal(EntityCategories.LabelOf(category, data.EntityNames), catalog.CharacterLabel(set));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.SupportedBuild)]
    [InlineData((byte)0x12, 6)] // Aquatics
    [InlineData((byte)0x13, 4)] // Ghosts
    public void The_two_grouped_categories_name_the_same_species_in_both_panels(byte category, int expected)
    {
        var catalog = OpenCatalog();
        Assert.True(TestPaths.TryGetGameData(out var data));

        var fromTree = AnimationTables.All.Values
            .Where(s => s.Category == category && s.RosterOrdinal is not null)
            .Select(catalog.CharacterLabel)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        var fromLayers = Enumerable.Range(0, expected)
            .Select(t => EntityCategories.SubtypeNameOf(category, (ushort)t, data.EntityNames) ?? "")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, fromTree.Count);
        Assert.Equal(fromLayers, fromTree);
    }

    /// <summary>Verifies that Pyrosaur names match between panels despite sharing category 0x09 with projectile art.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Pyrosaur_is_named_alike_despite_sharing_its_category_with_the_leftover_projectile()
    {
        var catalog = OpenCatalog();
        Assert.True(TestPaths.TryGetGameData(out var data));

        Assert.Equal(EntityCategories.LabelOf(0x09, data.EntityNames),
            catalog.CharacterLabel(AnimationTables.All["Pyrosaur"]));
    }

    /// <summary>Guards that species display order matches between the Resources tree and Layers panel.</summary>
    [Fact]
    public void Whole_category_species_sort_by_roster_ordinal()
    {
        byte[] rosterOrder = [0x06, 0x07, 0x08, 0x0C, 0x0D, 0x0A, 0x0F, 0x0E, 0x10, 0x09, 0x32];

        var sorted = rosterOrder.OrderBy(c => EntityCategories.EnemiesDisplayOrderOf(c, type: null)).ToArray();

        Assert.Equal(rosterOrder, sorted);
    }

    /// <summary>Verifies that categories without roster ordinals sort after cataloged species.</summary>
    [Fact]
    public void A_category_without_a_roster_ordinal_sorts_after_every_species()
    {
        var nirp = EntityCategories.EnemiesDisplayOrderOf(0x0A, type: null);
        var nirpling = EntityCategories.EnemiesDisplayOrderOf(0x0B, type: null);

        Assert.True(nirpling.CompareTo(nirp) > 0);
    }

    /// <summary>Verifies that Aquatics subtypes sort by species roster ordinal order.</summary>
    [Fact]
    public void Aquatics_sort_by_roster_ordinal_not_by_dlf_type()
    {
        var orderedOrdinals = Enumerable.Range(0, 6)
            .Select(t => EntityCategories.EnemiesDisplayOrderOf(0x12, (ushort)t).Anchor)
            .OrderBy(ordinal => ordinal)
            .ToArray();

        Assert.Equal([20, 21, 22, 26, 27, 28], orderedOrdinals);
    }

    /// <summary>Verifies that Ghosts sort by DLF subtype index order (0..3).</summary>
    [Fact]
    public void Ghosts_sort_by_dlf_type_not_by_roster_ordinal()
    {
        var orderedTypes = Enumerable.Range(0, 4)
            .Select(t => (Type: t, Order: EntityCategories.EnemiesDisplayOrderOf(0x13, (ushort)t)))
            .OrderBy(x => x.Order)
            .Select(x => x.Type)
            .ToArray();

        Assert.Equal([0, 1, 2, 3], orderedTypes);
    }
}
