namespace DigItExplorer.Core.Knowledge;

/// <summary>Resolves per-world sprite sheet filenames for platform colliders and character spawn poses.</summary>
internal static class WorldSheets
{
    /// <summary>Extracts the world index from a level file stem.</summary>
    private static int WorldOf(string stem)
        => stem.Length > 3 && char.IsDigit(stem[3]) ? (stem[3] - '0') / 2 : 0;

    /// <summary>Resolves a world-suffixed sprite sheet from the archive loader.</summary>
    public static byte[]? ResolveWorldSheet(string baseName, string stem, Func<string, byte[]?> loadSheet)
    {
        int world = WorldOf(stem);
        foreach (var name in new[] { $"{baseName}0{world}", baseName, $"{baseName}00" })
        foreach (var ext in new[] { ".SPF", ".MPF" })
        {
            var bytes = loadSheet(name + ext);
            if (bytes is not null) return bytes;
        }
        return null;
    }

    /// <summary>Resolves the world-costumed land spawn sprite sheet for the player.</summary>
    public static byte[]? ResolveDugSpawnSheet(string stem, Func<string, byte[]?> loadSheet)
        => loadSheet($"DUG{PlayerCostumes.CodeFor((World)WorldOf(stem))}B.SPF");
}
