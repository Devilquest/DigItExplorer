using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="BonusZones"/>'s per-level bonus-slot detection rules.</summary>
public class BonusZonesTests
{
    /// <summary>Verifies that water levels identify slot 9 as the bonus zone without reading DLF records.</summary>
    [Fact]
    public void GetBonusSlots_water_level_flags_slot_9_without_reading_any_dlf()
    {
        var stems = new SortedSet<string> { "LVL200", "LVL201", "LVL209" };
        var bonusSlots = BonusZones.GetBonusSlots("LVL20", stems,
            _ => throw new InvalidOperationException("water rule must not read DLF bytes"));
        Assert.Equal(new HashSet<int> { 9 }, bonusSlots);
    }

    /// <summary>Verifies that land level bonus zones match DLF warp records rather than door linkages.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void GetBonusSlots_land_level_matches_the_dlf_warp_records()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var stems = new SortedSet<string> { "LVL040", "LVL041", "LVL042", "LVL043" };
        var bonusSlots = BonusZones.GetBonusSlots("LVL04", stems,
            name =>
            {
                var path = name;
                return library.Contains(path) ? library.Read(path) : null;
            });

        Assert.Equal(new HashSet<int> { 1 }, bonusSlots);
        Assert.DoesNotContain(0, bonusSlots);
    }
}
