using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Defines labels, marker colors, and Layers-panel groupings for .DLF entity categories and types.</summary>
public static class EntityCategories
{
    // Species roster ordinals in the end-sequence gallery (EntityNames).
    private static readonly Dictionary<byte, int> RosterOrdinals = new()
    {
        [0x06] = 0,  // Slugger
        [0x07] = 1,  // Draggo
        [0x08] = 2,  // Rocker
        [0x09] = 38, // Pyrosaur
        [0x0A] = 8,  // Nirp
        [0x0C] = 6,  // Spurk
        [0x0D] = 7,  // Hopper
        [0x0E] = 33, // Papa Spurk
        [0x0F] = 32, // Troggi
        [0x10] = 37, // Grock
        [0x32] = 42, // Supreme Spurkasaur
    };

    // Fallback labels for items, markers, and aggregate categories (0x0B Nirpling is not named in MAIN.EXE).
    private static readonly Dictionary<byte, string> Labels = new()
    {
        // 0x04 Moving Platforms are named per-type in SubtypeNames.
        [0x00] = "Gold", [0x01] = "Silver", [0x02] = "Gem",
        [0x05] = "Dig Spot", [0x0B] = "Nirpling",
        [0x12] = "Aquatics", [0x13] = "Ghosts", [0x14] = "Falling Rock",
        [0x5A] = "Exit Sign", [0x5B] = "Drain", [0x5C] = "Plant", [0x63] = "Player Spawn", [0xFF] = "Player Spawn",
    };

    private static readonly Dictionary<byte, (byte R, byte G, byte B)> Colors = new()
    {
        [0x00] = (255, 215, 0),   // gold item
        [0x01] = (192, 192, 192), // silver item
        [0x02] = (0, 220, 255),   // gem
        [0x04] = (160, 90, 255),  // moving platform
        [0x05] = (255, 30, 30),  // dig-spot X (flat color, no type-dependent sub-colors)
        [0x06] = (255, 203, 0),   // Slugger
        [0x07] = (0, 231, 0),     // Draggo (no separate color for the Drakko reskin on underworld levels)
        [0x08] = (160, 0, 218),   // Rocker
        [0x09] = (215, 0, 0),     // Pyrosaur
        [0x0A] = (235, 0, 113),   // Nirp
        [0x0B] = (218, 65, 138),  // Nirpling
        [0x0C] = (0, 111, 0),     // Spurk
        [0x0D] = (0, 182, 255),   // Hopper
        [0x0E] = (0, 101, 154),   // Papa Spurk
        [0x0F] = (97, 0, 138),    // Troggi
        [0x10] = (159, 159, 159), // Grock
        [0x12] = (0, 140, 140),   // Aquatics
        [0x13] = (185, 185, 185), // Ghost
        [0x14] = (200, 120, 60),  // crumbling rocks
        [0x32] = (255, 255, 255), // boss
        [0x5A] = (255, 0, 0),     // water-level exit sign
        [0x5B] = (0, 90, 200),    // drain
        [0x5C] = (0, 100, 0),     // plant
        [0x63] = (0, 255, 120),   // player spawn (water levels)
        [0xFF] = (0, 255, 120),   // synthetic land player spawn
    };

    /// <summary>Returns the display name for a DLF category, falling back to hex format if unidentified.</summary>
    public static string LabelOf(byte category, EntityNames? names = null)
        => RosterOrdinals.TryGetValue(category, out int ordinal)
            ? names?[ordinal] ?? $"0x{category:X2}"
            : Labels.TryGetValue(category, out var label) ? label : $"0x{category:X2}";

    /// <summary>Flat marker color for a DLF category and type, or a neutral gray if unidentified.</summary>
    public static (byte R, byte G, byte B) ColorOf(byte category, ushort type = 0)
    {
        if (category == 0x05)
        {
            return type == 0 ? ((byte)255, (byte)0, (byte)0) : ((byte)255, (byte)200, (byte)0); // Exit X vs Bonus X
        }
        return Colors.TryGetValue(category, out var color) ? color : ((byte)200, (byte)200, (byte)200);
    }

    /// <summary>Which Layers-panel bucket a DLF category belongs in.</summary>
    public static EntityBucket BucketOf(byte category) => category switch
    {
        0x00 or 0x01 or 0x02 => EntityBucket.Goodies, // Gold and silver break down per type; gems stay one
                                                      // leaf because every gem has identical value.
        0x5C => EntityBucket.Decor, // plants: set-dressing / props (caves-only; per-type breakdown, see SubtypeNames)
        0x04 or 0x5B => EntityBucket.Mechanisms, // the two platform kinds and the drain
        0x05 or 0x5A or 0x63 or 0xFF => EntityBucket.Markers, // 0xFF = synthetic land-spawn, same as 0x63
        0x06 or 0x07 or 0x08 or 0x09 or 0x0A or 0x0B or 0x0C or 0x0D or
            0x0E or 0x0F or 0x10 or 0x12 or 0x13 or 0x14 or 0x32 => EntityBucket.Enemies,
        _ => EntityBucket.Misc,
    };

    /// <summary>Default sticky visibility for a freshly-seen category: every entity category starts on
    /// (only the diagnostic Collision layer defaults off; see MainWindow's <c>_collisionLayerVisible</c>).</summary>
    public static bool DefaultVisible(byte category) => true;

    /// <summary>Per-type display names for goodies, plant decor, platforms, and dig spots.</summary>
    private static readonly Dictionary<byte, string[]> SubtypeNames = new()
    {
        [0x00] = ["Extra Dug", "Shovel", "Pickaxe", "Diving Helmet", "Gold Goodie (unused)", "Torch"],
        [0x5C] = ["Vine (gold)", "Vine (silver)", "Berry Bush (gold)", "Berry Bush (green)",
            "Fruit Flower (red)", "Fruit Flower (gold)", "Curl Weed (gold)", "Curl Weed (silver)", "Yellow Blossom"],
        [0x01] = ["Energy", "Spring Boots", "Rapid Fire", "Jet Pack", "Time", "Super Dug", "Trail Mark"],
        [0x04] = ["Moving Platform", "Falling Platform"],
        [0x05] = ["Dig Spot (exit)", "Dig Spot (bonus)"],
    };

    /// <summary>Subtype roster ordinals for Aquatic and Ghost enemy species families.</summary>
    private static readonly Dictionary<byte, int[]> SubtypeOrdinals = new()
    {
        [0x12] = [22, 21, 27, 20, 28, 26], // Rockerfish, Sea Draggo, Hopperfish, Aqua Slugger, Nirpies, Sea Spurk
        [0x13] = [12, 14, 13, 15],         // Ghost Slugger, Ghost Draggo, Ghost Rocker, Ghost Pyrosaur
    };

    /// <summary>Determines whether a category decomposes into individual subtype layers.</summary>
    public static bool HasSubtypeBreakdown(byte category)
        => SubtypeNames.ContainsKey(category) || SubtypeOrdinals.ContainsKey(category);

    /// <summary>Returns the subtype display name for a (category, type) pair.</summary>
    public static string? SubtypeNameOf(byte category, ushort type, EntityNames? names = null)
    {
        if (SubtypeOrdinals.TryGetValue(category, out var ordinals))
            return type < ordinals.Length ? names?[ordinals[type]] : null;
        return SubtypeNames.TryGetValue(category, out var labels) && type < labels.Length ? labels[type] : null;
    }

    /// <summary>Calculates sorting order for enemy layers matching resource tree hierarchy.</summary>
    public static (int Anchor, int WithinFamily) EnemiesDisplayOrderOf(byte category, ushort? type)
    {
        if (category == 0x13)
            return (SubtypeOrdinals[0x13][0], type ?? 0);
        if (type is ushort t && SubtypeOrdinals.TryGetValue(category, out var ordinals) && t < ordinals.Length)
            return (ordinals[t], 0);
        return (RosterOrdinals.TryGetValue(category, out int ordinal) ? ordinal : int.MaxValue, 0);
    }

    /// <summary>Returns the unique layer grouping key for a DLF entity record.</summary>
    public static (byte Category, ushort? Type) KeyOf(DlfRecord e)
    {
        if (e.Category == 0x05)
        {
            ushort groupedType = e.Type == 0 ? (ushort)0 : (ushort)1;
            return (e.Category, groupedType);
        }
        return HasSubtypeBreakdown(e.Category) ? (e.Category, e.Type) : (e.Category, null);
    }
}

/// <summary>Categories for grouping entity layers in the UI.</summary>
public enum EntityBucket
{
    Mechanisms, // Moving/falling platforms (0x04) and drains (0x5B)
    Enemies,
    Goodies,    // Gold and silver items (0x00, 0x01) and gems (0x02)
    Decor,      // Plants (0x5C)
    Markers,    // Dig spots (0x05), exit signs (0x5A), and player spawns (0x63, 0xFF)
    Misc,
}
