using System.Text;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Guards standalone data fields in GameData read from the executable.</summary>
public class GameDataTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Find_it_start_label_is_a_well_formed_string()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        Assert.NotEmpty(data.FindItStartLabel);
        Assert.All(data.FindItStartLabel, b => Assert.InRange(b, (byte)' ', (byte)'~'));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Find_it_start_label_reads_the_text_this_build_ships() // build fingerprint, not a guard: see the class summary
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        Assert.Equal("Start", Encoding.Latin1.GetString(data.FindItStartLabel));
    }
}
