using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Tests;

/// <summary>Guards the completeness of <see cref="AnimationTables.All"/> roster registrations.</summary>
public class AnimationTablesTests
{
    [Fact]
    public void All_contains_exactly_the_expected_roster()
    {
        var expected = new[]
        {
            "Slugger", "Draggo", "Rocker", "Spurk", "Hopper", "Nirp", "Nirpling",
            "Ghost Slugger", "Ghost Draggo", "Ghost Rocker", "Ghost Pyrosaur",
            "Aqua Slugger", "Sea Draggo", "Rockerfish", "Sea Spurk", "Hopperfish", "Nirpies",
            "Troggi", "Papa Spurk", "Grock", "Pyrosaur", "Supreme Spurkasaur",
            "Dug", "DugCrouch", "DugWait", "DugDig", "DugSuper", "DugJetpack", "DugSwim", "DugDirt", "DugMap",
            "Fireball", "Fireball (unused)", "General Effects", "Hit", "Sparkles",
            "Flip It!", "Spin It!",
            "Gold Goodies", "Silver Goodies", "Gems",
            "Plant", "Bubble", "Power-up Banners", "Status Icons", "HUD (General)",
            "Stop It! Pieces", "Stop It! Prize Icons", "Find It! Pieces", "Find It! Prize Icons",
            "Flip It! Prize Icons",
            "Falling Rock", "Nirp Egg", "Snowball",
        };

        Assert.Equal(expected.Length, AnimationTables.All.Count);
        foreach (var name in expected)
            Assert.True(AnimationTables.All.ContainsKey(name), $"missing roster entry: {name}");
    }
}
