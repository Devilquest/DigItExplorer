using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="WorldSheets"/>'s per-world sheet resolution fallback order.</summary>
public class WorldSheetsTests
{
    private static Func<string, byte[]?> LoaderOf(params string[] present)
    {
        var set = new HashSet<string>(present, StringComparer.OrdinalIgnoreCase);
        // Each present name returns a distinct one-byte payload (its own name's first byte) so a test
        // can confirm exactly *which* candidate was picked, not just that something was returned.
        return name => set.Contains(name) ? [(byte)(name.GetHashCode() & 0xFF)] : null;
    }

    [Theory]
    [InlineData("LVL000", 0)] // caves
    [InlineData("LVL200", 1)] // water
    [InlineData("LVL400", 2)] // snow
    [InlineData("LVL500", 2)] // snow overflow block
    [InlineData("LVL600", 3)] // underworld
    [InlineData("LVL700", 3)] // underworld overflow block
    public void ResolveWorldSheet_prefers_the_world_suffixed_spf(string stem, int world)
    {
        string suffixedName = $"WO_DRG0{world}.SPF";
        var loader = LoaderOf(suffixedName, $"WO_DRG0{world}.MPF", "WO_DRG.SPF", "WO_DRG00.SPF");
        var bytes = WorldSheets.ResolveWorldSheet("WO_DRG", stem, loader);
        Assert.Equal(loader(suffixedName), bytes);
    }

    [Fact]
    public void ResolveWorldSheet_prefers_spf_over_mpf_for_the_same_name()
    {
        var calls = new List<string>();
        Func<string, byte[]?> loader = name =>
        {
            calls.Add(name);
            return name == "WO_DRG00.MPF" ? [1] : null; // only the MPF variant exists
        };
        var bytes = WorldSheets.ResolveWorldSheet("WO_DRG", "LVL000", loader);
        Assert.NotNull(bytes);
        Assert.Equal(["WO_DRG00.SPF", "WO_DRG00.MPF"], calls); // SPF tried first, MPF second
    }

    [Fact]
    public void ResolveWorldSheet_falls_back_to_the_bare_name_then_caves()
    {
        var bareOnly = LoaderOf("WO_DRG.SPF");
        var bytes = WorldSheets.ResolveWorldSheet("WO_DRG", "LVL600", bareOnly); // underworld, no 03 file
        Assert.NotNull(bytes);

        var cavesOnly = LoaderOf("WO_DRG00.SPF");
        var bytes2 = WorldSheets.ResolveWorldSheet("WO_DRG", "LVL600", cavesOnly); // no 03, no bare -> caves "00"
        Assert.NotNull(bytes2);
    }

    [Fact]
    public void ResolveWorldSheet_returns_null_when_nothing_matches()
    {
        Assert.Null(WorldSheets.ResolveWorldSheet("WO_DRG", "LVL600", _ => null));
    }

    [Theory]
    [InlineData("LVL000", "DUG0B.SPF")] // caves -> '0'
    [InlineData("LVL200", "DUG0B.SPF")] // water -> falls back to caves' '0'
    [InlineData("LVL400", "DUG6B.SPF")] // snow -> '6'
    [InlineData("LVL600", "DUG3B.SPF")] // underworld -> '3'
    [InlineData("LV", "DUG0B.SPF")]     // stem too short to read a world digit -> defaults to '0'
    public void ResolveDugSpawnSheet_picks_the_worlds_costume_digit(string stem, string expectedName)
    {
        string? requested = null;
        Func<string, byte[]?> loader = name => { requested = name; return [1]; };
        WorldSheets.ResolveDugSpawnSheet(stem, loader);
        Assert.Equal(expectedName, requested);
    }
}
