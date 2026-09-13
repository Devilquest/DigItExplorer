using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Simulates player and boss drop landings against collision surfaces.</summary>
internal static class LandingSimulator
{
    /// <summary>Synthetic category for the injected land-spawn record.</summary>
    public const byte CatPlayer = 0xFF;

    // Land levels have no spawn record: the loader hardcodes player = (40, -40) facing right for every
    // level and gravity does the rest; only water levels override it with a real category-0x63 record.
    private const int PlayerSpawnX = 40;
    private static readonly HashSet<byte> PlayerStandable = [64, 112]; // walkable surface / floor codes
    // The land-spawn rect's inclusive (128,38,159,73) -> 32x36; only the footprint size matters for the drop.
    private const int PlayerSpriteW = 32, PlayerSpriteH = 36;

    /// <summary>Calculates the resting position for the player spawn dropping onto standable tiles.</summary>
    /// <returns>Calculated (X, Y) landing coordinates, or null if no standable floor is found.</returns>
    public static (int X, int Y)? PlayerLanding(CollisionImage collision)
    {
        var codes = collision.MaterialCodes;
        int stride = collision.Width;
        int maxX = Math.Min(PlayerSpawnX + PlayerSpriteW, collision.Width);

        for (int y = 0; y < collision.Height; y++)
        {
            int rowOff = y * stride;
            for (int x = PlayerSpawnX; x < maxX; x++)
            {
                if (PlayerStandable.Contains(codes[rowOff + x]))
                    return (PlayerSpawnX, y - PlayerSpriteH - 1);
            }
        }
        return null;
    }

    // The LVL750 boss record's x/y are dummy (0, 0): the init routine hardcodes (240, -100), then falls
    // 12px/tick probing (X+46, Y+70) for an edge-line M-code (16..31); rest Y = probe row - 70.
    private const int BossSpawnX = 240, BossSpawnY = -100;
    private const int BossProbeX = 46, BossProbeY = 70;

    /// <summary>Calculates the resting position for the boss spawn dropping onto edge-line tiles.</summary>
    /// <returns>Calculated (X, Y) landing coordinates, or null if no edge floor is found.</returns>
    public static (int X, int Y)? BossLanding(CollisionImage collision)
    {
        int px = BossSpawnX + BossProbeX;
        if (px >= collision.Width) return null;

        var codes = collision.MaterialCodes;
        int stride = collision.Width;
        int startY = Math.Max(0, BossSpawnY + BossProbeY);

        for (int y = startY; y < collision.Height; y++)
        {
            if ((codes[y * stride + px] & 0xF0) == 0x10)
                return (BossSpawnX, y - BossProbeY);
        }
        return null;
    }

    /// <summary>Injects simulated landing positions for player and boss spawns into DLF records.</summary>
    public static IReadOnlyList<DlfRecord> SynthesizeSpawns(IReadOnlyList<DlfRecord> records, CollisionImage collision, string levelStem)
    {
        var result = new List<DlfRecord>(records);

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].Category != 0x32) continue;
            if (BossLanding(collision) is { } boss)
                result[i] = result[i] with { X = (short)boss.X, Y = (short)boss.Y, P0 = 0xFFFF };
            break; // exactly one boss record per level
        }

        if (levelStem != "LVL900" && !result.Any(r => r.Category == 0x63) && PlayerLanding(collision) is { } player)
            result.Add(new DlfRecord(CatPlayer, 0, (short)player.X, (short)player.Y, 1, 0, 0, 0, 0));

        return result;
    }
}
