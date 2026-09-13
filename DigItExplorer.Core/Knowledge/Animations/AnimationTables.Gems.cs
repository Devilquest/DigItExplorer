namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Collectible gem variants (WO_GEMS, 20 types, category 0x02).</summary>
    public static readonly CharacterAnimSet Gems = new("Gems", null,
        BuildGemAnims(),
        FixedSheet: "WO_GEMS",
        FixedPalette: "LVL000.PAL",
        FixedLocation: "Goodies",
        SlicedSheet: true);

    private const int GemVariantCount = 20;

    /// <summary>Constructs animation definitions for all gem variants.</summary>
    private static AnimationDef[] BuildGemAnims()
    {
        static bool IsUnplaced(int type) => type is 6 or 7 or 9 or 11 or 19;

        var anims = new AnimationDef[GemVariantCount];
        for (int type = 0; type < anims.Length; type++)
            anims[type] = new AnimationDef($"gem_{type}", [type], AnimMode.Pose,
                IsUnplaced(type) ? $"type {type}; sheet art exists but no shipped level spawns it"
                                 : $"type {type}",
                DisplayName: IsUnplaced(type) ? $"Gem {type} (unused)" : null);
        return anims;
    }
}
