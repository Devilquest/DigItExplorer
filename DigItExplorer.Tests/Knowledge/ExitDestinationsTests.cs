using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="ExitDestinations"/>'s world/node numbering and water-substage lookup.</summary>
public class ExitDestinationsTests
{
    [Theory]
    [InlineData("LVL000", -1, "MAP")]
    [InlineData("LVL000", 65535, "MAP")]
    [InlineData("LVL000", 5, "005")]      // digit '0' -> already even -> 0
    [InlineData("LVL100", 3, "003")]      // digit '1' -> rounds down to 0
    [InlineData("LVL200", 7, "207")]      // digit '2' -> already even -> 2
    [InlineData("LVL300", 50, "250")]     // digit '3' -> rounds down to 2
    [InlineData("LVL000", 105, "105")]    // slot >= 100 -> digit bumped by one, 100 stripped
    [InlineData("LV", 5, "005")]          // too short to read the world digit -> defaults to 0
    public void GetDestLabel_matches_the_worldnode_numbering(string levelStem, int slot, string expected)
    {
        Assert.Equal(expected, ExitDestinations.GetDestLabel(levelStem, slot));
    }

    [Theory]
    [InlineData("LVL040", 40, true)]      // a Spookstone wrong door: back into the room it stands in
    [InlineData("LVL040", 42, false)]     // onward, to another room of the same house
    [InlineData("LVL040", 65535, false)]  // completes the level, so it leads out rather than back
    [InlineData("LVL040", -1, false)]
    [InlineData("LVL440", 44, false)]     // 'LVL440' + slot 44 resolves to LVL444, not to itself
    [InlineData("LVL440", 40, true)]
    [InlineData("LVL630", 30, true)]
    [InlineData("LVL630", 31, false)]
    [InlineData("LV", 0, false)]          // too short to hold a level number
    public void LeadsBackToItself_spots_a_door_that_returns_to_its_own_level(string levelStem, int slot, bool expected)
    {
        Assert.Equal(expected, ExitDestinations.LeadsBackToItself(levelStem, slot));
    }

    [Fact]
    public void NextWaterSlot_returns_the_following_substage_when_it_exists()
    {
        int slot = ExitDestinations.NextWaterSlot("LVL209",
            name => name.Equals("LVL210.DLF", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(10, slot);
    }

    [Fact]
    public void NextWaterSlot_returns_minus_one_when_the_next_substage_is_absent()
    {
        int slot = ExitDestinations.NextWaterSlot("LVL209", _ => false);
        Assert.Equal(-1, slot);
    }

    [Fact]
    public void NextWaterSlot_returns_minus_one_for_non_water_levels()
    {
        int slot = ExitDestinations.NextWaterSlot("LVL009", _ => true);
        Assert.Equal(-1, slot);
    }

    [Fact]
    public void NextWaterSlot_returns_minus_one_for_a_too_short_stem()
    {
        int slot = ExitDestinations.NextWaterSlot("LVL2", _ => true);
        Assert.Equal(-1, slot);
    }

    [Theory]
    [InlineData(0x05, 7, 0, 7)]      // dig spot: param 0
    [InlineData(0x5B, 1, 9, 9)]      // drain: param 1, since param 0 is its facing flag
    public void DestinationSlotOf_reads_the_slot_each_marker_keeps_it_in(byte category, ushort p0, ushort p1,
        int expected)
    {
        var record = new DlfRecord(category, Type: 0, X: 0, Y: 0, p0, p1, P2: 0, P3: 0, P4: 0);
        Assert.Equal(expected, ExitDestinations.DestinationSlotOf(record, "LVL200", _ => false));
    }

    [Fact]
    public void DestinationSlotOf_takes_the_water_exit_signs_slot_from_the_levels_that_follow_it()
    {
        var sign = new DlfRecord(Category: 0x5A, Type: 0, X: 0, Y: 0, P0: 0xFFFF, P1: 0, P2: 0, P3: 0, P4: 0);
        Assert.Equal(1, ExitDestinations.DestinationSlotOf(sign, "LVL200", name => name == "LVL201.DLF"));
    }

    [Fact]
    public void DestinationSlotOf_returns_nothing_for_a_record_that_is_not_an_exit()
    {
        var enemy = new DlfRecord(Category: 0x0C, Type: 0, X: 0, Y: 0, P0: 1, P1: 0, P2: 0, P3: 0, P4: 0);
        Assert.Null(ExitDestinations.DestinationSlotOf(enemy, "LVL200", _ => false));
    }

    /// <summary>A drain going in reaches a bonus zone and the same drain coming back does not, so the two
    /// directions of one object are told apart by the level each arrives at.</summary>
    [Fact]
    public void LeadsToBonusZone_follows_the_destinations_own_header()
    {
        Assert.True(ExitDestinations.LeadsToBonusZone("LVL200", 9,
            name => name == "LVL209.DLF" ? LevelHeader(bonus: true) : null));
        Assert.False(ExitDestinations.LeadsToBonusZone("LVL209", 0,
            name => name == "LVL200.DLF" ? LevelHeader(bonus: false) : null));
    }

    [Fact]
    public void LeadsToBonusZone_is_false_where_the_exit_leads_out_of_the_level_or_back_into_it()
    {
        Assert.False(ExitDestinations.LeadsToBonusZone("LVL040", 65535, _ => LevelHeader(bonus: true)));
        Assert.False(ExitDestinations.LeadsToBonusZone("LVL040", 40, _ => LevelHeader(bonus: true)));
    }

    [Fact]
    public void LeadsToBonusZone_is_false_where_the_destination_cannot_be_read()
    {
        Assert.False(ExitDestinations.LeadsToBonusZone("LVL200", 9, _ => null));
        Assert.False(ExitDestinations.LeadsToBonusZone("LVL200", 9, _ => new byte[8]));
    }

    /// <summary>Verifies that a drain reaches a bonus zone from an ordinary water level and an ordinary
    /// substage from inside a bonus zone, across every water level the copy of the game holds.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void A_drain_leads_into_a_bonus_zone_only_from_outside_one()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        int drains = 0;
        foreach (var name in library.Names.Where(IsWaterLevelList))
        {
            var dlf = library.Read(name);
            string stem = name[..^4];
            bool insideBonusZone = DlfHeader.Read(dlf).IsBonus;
            foreach (var record in DlfRecord.ReadAll(dlf).Where(r => r.Category == 0x5B))
            {
                int slot = ExitDestinations.DestinationSlotOf(record, stem, library.Contains)!.Value;
                Assert.Equal(!insideBonusZone,
                    ExitDestinations.LeadsToBonusZone(stem, slot, library.TryRead));
                drains++;
            }
        }
        Assert.True(drains >= 10, $"test assumption: the water world holds drains in both directions, saw {drains}");
    }

    private static bool IsWaterLevelList(string name)
        => name.StartsWith("LVL2", StringComparison.OrdinalIgnoreCase)
           && name.EndsWith(".DLF", StringComparison.OrdinalIgnoreCase);

    /// <summary>A level file holding nothing but the header word that says whether it is a bonus zone.</summary>
    private static byte[] LevelHeader(bool bonus)
    {
        var dlf = new byte[DlfHeader.SizeWithBonus];
        dlf[10] = (byte)(bonus ? 1 : 0);
        return dlf;
    }
}
