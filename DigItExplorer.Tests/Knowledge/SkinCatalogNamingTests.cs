using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Tests;

/// <summary>Guards the animation display-name derivation and its per-character overrides.</summary>
public sealed class SkinCatalogNamingTests
{
    [Theory]
    [InlineData("walk", "Walk")]
    [InlineData("rope_climb", "Rope climb")]
    [InlineData("dig_windup", "Dig windup")]
    public void A_key_name_becomes_one_sentence_case_label(string key, string expected)
        => Assert.Equal(expected, SkinCatalog.PrettyAnimName("Slugger", key));

    [Theory]
    [InlineData("idle_wait_frames", "Idle wait")]
    [InlineData("idle_wait_sim", "Idle wait (simulated)")]
    [InlineData("swim_left_map", "Swim left")]
    public void An_override_is_sentence_case_too(string key, string expected)
        => Assert.Equal(expected, SkinCatalog.PrettyAnimName("Dug", key));

    [Fact]
    public void The_ghost_override_reaches_every_ghost()
    {
        var ghosts = AnimationTables.All.Values.Where(set => set.Category == GhostCategory).ToList();

        Assert.NotEmpty(ghosts);
        foreach (var ghost in ghosts)
            Assert.Equal("Walk/idle", SkinCatalog.PrettyAnimName(ghost.Name, "idle"));
    }

    [Fact]
    public void The_ghost_override_reaches_nothing_else()
    {
        // The same key means something else for a character that is not a ghost, so an override applied by
        // key alone would mislabel every one of them.
        foreach (var other in AnimationTables.All.Values.Where(set => set.Category != GhostCategory))
            Assert.Equal("Idle", SkinCatalog.PrettyAnimName(other.Name, "idle"));
    }

    [Fact]
    public void A_name_that_merely_begins_with_the_word_is_not_a_ghost()
        // Not a character the game has: a probe for the space in the prefix, without which any name
        // starting with those five letters would take the override.
        => Assert.Equal("Idle", SkinCatalog.PrettyAnimName("Ghostbuster", "idle"));

    // Taken from a known ghost rather than written out, so both scopes follow the roster.
    private static readonly byte? GhostCategory = AnimationTables.GhostSlugger.Category;
}
