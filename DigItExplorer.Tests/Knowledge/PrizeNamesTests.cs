using System.Text;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Guards the executable bonus prize string tables and lookup indices.</summary>
public class PrizeNamesTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Both_tables_still_land_on_five_well_formed_strings()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var names = data.PrizeNames;
        foreach (Prize prize in Enum.GetValues<Prize>())
        {
            var shortName = names.Short(prize);
            var longName = names.Long(prize);
            Assert.NotEmpty(shortName);
            Assert.NotEmpty(longName);
            Assert.All(shortName, b => Assert.InRange(b, (byte)' ', (byte)'~'));
            Assert.All(longName, b => Assert.InRange(b, (byte)' ', (byte)'~'));
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Tables_read_the_names_this_build_ships() // build fingerprint, not a guard: see the class summary
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var names = data.PrizeNames;
        Assert.Equal("Draggo!", Encoding.Latin1.GetString(names.Short(Prize.Draggo)));
        Assert.Equal("Extra", Encoding.Latin1.GetString(names.Short(Prize.ExtraDug)));
        Assert.Equal("25", Encoding.Latin1.GetString(names.Short(Prize.Gems25)));
        Assert.Equal("50", Encoding.Latin1.GetString(names.Short(Prize.Gems50)));
        Assert.Equal("Energy", Encoding.Latin1.GetString(names.Short(Prize.Energy)));

        Assert.Equal("Draggo!", Encoding.Latin1.GetString(names.Long(Prize.Draggo)));
        Assert.Equal("25 Gems", Encoding.Latin1.GetString(names.Long(Prize.Gems25)));
        Assert.Equal("50 Gems", Encoding.Latin1.GetString(names.Long(Prize.Gems50)));
    }
}
