using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Guards the executable species roster table and category label derivations.</summary>
public class EntityNamesTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Roster_address_still_lands_on_a_well_formed_table()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var names = data.EntityNames;
        Assert.Equal(44, names.Count);

        for (int i = 0; i < names.Count; i++)
        {
            var entry = names[i];
            Assert.False(string.IsNullOrWhiteSpace(entry));
            Assert.All(entry!, c => Assert.InRange(c, ' ', '~'));
        }

        // The gallery alternates runs of species names with runs of their mock-Latin subtitles, and only the
        // subtitles are parenthesized. A table that had slipped by a string or two would mix the two shapes.
        Assert.DoesNotContain('(', names[0]!);
        Assert.StartsWith("(", names[3]);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Roster_reads_the_names_this_build_ships() // build fingerprint, not a guard: see the class summary
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var names = data.EntityNames;
        Assert.Equal("Slugger", names[0]);
        Assert.Equal("Nirp", names[8]);
        Assert.Equal("Ghost Rocker", names[13]);
        Assert.Equal("Rockerfish", names[22]);
        Assert.Equal("Drakko", names[36]);
        Assert.Equal("Supreme Spurkasaur", names[42]);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_species_category_is_labeled_from_the_roster()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        byte[] species = [0x06, 0x07, 0x08, 0x09, 0x0A, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x32];
        foreach (var category in species)
        {
            var label = EntityCategories.LabelOf(category, data.EntityNames);
            Assert.NotEqual($"0x{category:X2}", label);
            Assert.Contains(label, Enumerable.Range(0, data.EntityNames.Count).Select(i => data.EntityNames[i]));
        }
    }

    [Fact]
    public void Species_fall_back_to_their_category_byte_when_the_roster_is_unreadable()
    {
        // The failure this rule asks for: no roster means no species name, not a name of our own that the user
        // could not tell apart from one the game authored.
        Assert.Equal("0x07", EntityCategories.LabelOf(0x07, names: null));
        Assert.Null(EntityCategories.SubtypeNameOf(0x13, type: 0, names: null));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Nirpling_is_ours_and_stays_ours()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        // The one species the game names nowhere, not even in the gallery that names the Drakko reskin, so
        // its label is a name of this project's and does not come from, or vary with, the executable.
        Assert.Equal("Nirpling", EntityCategories.LabelOf(0x0B, data.EntityNames));
        Assert.Equal("Nirpling", EntityCategories.LabelOf(0x0B, names: null));
        Assert.DoesNotContain("Nirpling",
            Enumerable.Range(0, data.EntityNames.Count).Select(i => data.EntityNames[i]));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Subtype_leaves_of_the_species_groups_come_from_the_roster()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        // Ordinals, not text: the roster lists the ghosts in a different order than the DLF type indexes them,
        // so a positional read would silently swap Rocker and Draggo.
        Assert.Equal("Ghost Draggo", EntityCategories.SubtypeNameOf(0x13, 1, data.EntityNames));
        Assert.Equal("Ghost Rocker", EntityCategories.SubtypeNameOf(0x13, 2, data.EntityNames));
        Assert.Equal("Rockerfish", EntityCategories.SubtypeNameOf(0x12, 0, data.EntityNames));
        Assert.Equal("Aqua Slugger", EntityCategories.SubtypeNameOf(0x12, 3, data.EntityNames));
    }
}
