namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Gold pickup item animations (WO_G&amp;S row 1, category 0x00).</summary>
    public static readonly CharacterAnimSet GoldItems = new("Gold Goodies", null,
        BuildGoldAnims(),
        FixedSheet: "WO_G&S",
        FixedPalette: "LVL000.PAL",
        FixedLocation: "Goodies",
        SlicedSheet: true);

    /// <summary>Constructs animation definitions for all gold pickup types.</summary>
    private static AnimationDef[] BuildGoldAnims()
    {
        (string Key, string? Note)[] types =
        [
            ("extra_dug", "an extra life"),
            ("shovel", null),
            ("pickaxe", null),
            ("diving_helmet", null),
            ("gold_goodie_unused", "sheet art exists but no shipped level spawns it"),
            ("torch", null),
        ];

        var anims = new AnimationDef[types.Length];
        for (int type = 0; type < anims.Length; type++)
        {
            var (key, note) = types[type];
            anims[type] = new AnimationDef(key, [type], AnimMode.Pose,
                note is null ? $"type {type}" : $"type {type}; {note}",
                DisplayName: EntityCategories.SubtypeNameOf(0x00, (ushort)type)!);
        }
        return anims;
    }
}
