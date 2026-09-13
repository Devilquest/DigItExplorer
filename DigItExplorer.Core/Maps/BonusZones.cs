using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Detects which substage slots of a level node are bonus zones.</summary>
public static class BonusZones
{
    /// <summary>Identifies bonus substage slots based on faint warp destination targets.</summary>
    /// <param name="prefix">Level node 5-character stem prefix.</param>
    /// <param name="stems">Substage stems belonging to the level node.</param>
    /// <param name="loadDlf">Resolver function for level DLF record bytes.</param>
    /// <returns>Set of target substage slot indices that are bonus warps.</returns>
    public static HashSet<int> GetBonusSlots(string prefix, IReadOnlyCollection<string> stems, Func<string, byte[]?> loadDlf)
    {
        var bonusSlots = new HashSet<int>();
        bool isWater = prefix.StartsWith("LVL2", StringComparison.Ordinal);

        if (isWater)
        {
            foreach (var stem in stems)
            {
                if (stem.Length >= 6 && stem[5] == '9')
                {
                    bonusSlots.Add(9);
                }
            }
        }
        else
        {
            var presentSlots = new HashSet<int>();
            foreach (var stem in stems)
            {
                if (stem.Length >= 6 && char.IsDigit(stem[5]))
                {
                    presentSlots.Add(stem[5] - '0');
                }
            }

            foreach (var stem in stems)
            {
                var dlfBytes = loadDlf($"{stem}.DLF");
                if (dlfBytes is null) continue;

                foreach (var rec in DlfRecord.ReadAll(dlfBytes))
                {
                    if (rec.Category != 0x05 || rec.Type == 0) continue; // only faint bonus warps
                    if (rec.P0 == 65535) continue;                       // -1 = completes level, not a warp
                    int destSlot = rec.P0 % 10;
                    if (destSlot != 0 && presentSlots.Contains(destSlot))
                    {
                        bonusSlots.Add(destSlot);
                    }
                }
            }
        }

        return bonusSlots;
    }
}
