using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Computes destination labels for exit, bonus, and water spawn markers.</summary>
internal static class ExitDestinations
{
    /// <summary>Returns the destination label for an exit or spawn marker (e.g. 'MAP' or '001').</summary>
    /// <param name="levelStem">The level file stem (e.g. 'LVL000').</param>
    /// <param name="slot">Destination slot index (-1/65535 for world map, >=100 for alternate pair).</param>
    /// <returns>A formatted destination string or 'MAP'.</returns>
    public static string GetDestLabel(string levelStem, int slot)
    {
        if (IsWorldMap(slot))
        {
            return "MAP";
        }
        int digit = 0;
        if (levelStem.Length > 3 && char.IsDigit(levelStem[3]))
        {
            digit = levelStem[3] - '0';
        }
        digit -= digit % 2;
        if (slot >= 100)
        {
            digit += 1;
            slot -= 100;
        }
        return $"{digit}{slot:D2}";
    }

    /// <summary>Whether a destination slot ends the level at the world map rather than leading into
    /// another one.</summary>
    /// <param name="slot">Destination slot index (-1/65535 for world map).</param>
    public static bool IsWorldMap(int slot) => slot == -1 || slot == 65535;

    /// <summary>Whether a destination slot leads back into the level the marker stands in, which the wrong
    /// doors of a Spookstone maze do and no ordinary level's exit does.</summary>
    /// <param name="levelStem">The level file stem (e.g. 'LVL040').</param>
    /// <param name="slot">Destination slot index.</param>
    public static bool LeadsBackToItself(string levelStem, int slot)
        => !IsWorldMap(slot) && levelStem.Length > 3
           && string.Equals(GetDestLabel(levelStem, slot), levelStem[3..], StringComparison.Ordinal);

    /// <summary>Returns the destination slot an exit marker names, or null when the record is not one.</summary>
    /// <param name="record">A level record of any category.</param>
    /// <param name="levelStem">The level file stem the record belongs to (e.g. 'LVL209').</param>
    /// <param name="resourceExists">Predicate checking if a named resource exists.</param>
    public static int? DestinationSlotOf(DlfRecord record, string levelStem, Func<string, bool> resourceExists)
        => record.Category switch
        {
            0x05 => record.P0,                                          // dig spot
            0x5B => record.P1,                                          // drain
            0x5A => NextWaterSlot(levelStem, resourceExists),           // water exit sign
            _ => null,
        };

    /// <summary>Whether a destination slot leads into a bonus zone, which its own header declares.</summary>
    /// <param name="levelStem">The level file stem the marker stands in (e.g. 'LVL209').</param>
    /// <param name="slot">Destination slot index.</param>
    /// <param name="loadResource">Resolver function for archive resource bytes.</param>
    public static bool LeadsToBonusZone(string levelStem, int slot, Func<string, byte[]?> loadResource)
    {
        if (IsWorldMap(slot) || LeadsBackToItself(levelStem, slot)) return false;
        var dlf = loadResource($"LVL{GetDestLabel(levelStem, slot)}.DLF");
        return dlf is { Length: >= DlfHeader.SizeWithBonus } && DlfHeader.Read(dlf).IsBonus;
    }

    /// <summary>Determines the next sequential water-world substage slot if it exists.</summary>
    /// <param name="levelStem">The level file stem to advance from.</param>
    /// <param name="resourceExists">Predicate checking if the next .DLF file exists.</param>
    /// <returns>The next substage slot number, or -1 if none exists.</returns>
    public static int NextWaterSlot(string levelStem, Func<string, bool> resourceExists)
    {
        if (levelStem.StartsWith("LVL2", StringComparison.Ordinal) && levelStem.Length >= 6
            && int.TryParse(levelStem.Substring(levelStem.Length - 2), out int currentSlot))
        {
            int nextSlot = currentSlot + 1;
            string prefix = levelStem.Substring(0, levelStem.Length - 2);
            string nextLevelStem = $"{prefix}{nextSlot:D2}";
            if (resourceExists($"{nextLevelStem}.DLF"))
            {
                return nextSlot;
            }
        }
        return -1;
    }
}
