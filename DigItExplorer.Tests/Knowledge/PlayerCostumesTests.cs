using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Verifies player costume code mapping across worlds and unassigned costume handling in <see cref="PlayerCostumes"/>.</summary>
public class PlayerCostumesTests
{
    [Fact]
    public void Costume_digits_resolve_per_world_with_water_wearing_the_caves_suit()
    {
        Assert.Equal('0', PlayerCostumes.CodeFor(World.Caves));
        Assert.Equal('6', PlayerCostumes.CodeFor(World.Snow));
        Assert.Equal('3', PlayerCostumes.CodeFor(World.Underworld));
        Assert.Equal('0', PlayerCostumes.CodeFor(World.Water));
    }

    [Fact]
    public void The_abandoned_costume_belongs_to_no_world()
    {
        // Costume '5' shares caves' palette purely as an extraction default, so it must not be reachable
        // through a world lookup: that would put an unfinished sheet on screen labeled as a real world's.
        var unused = Assert.Single(PlayerCostumes.All, c => c.Code == '5');
        Assert.Null(unused.World);
        Assert.DoesNotContain(PlayerCostumes.All.Where(c => c.World is not null), c => c.Code == '5');
    }
}
