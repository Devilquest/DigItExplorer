namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Silver pickup item animations (WO_G&amp;S row 2, category 0x01).</summary>
    public static readonly CharacterAnimSet SilverItems = new("Silver Goodies", null,
        BuildSilverAnims(),
        FixedSheet: "WO_G&S",
        FixedPalette: "LVL000.PAL",
        FixedLocation: "Goodies",
        SlicedSheet: true);

    /// <summary>The sheet cell the silver row starts at, the gold row occupying the six before it.</summary>
    private const int SilverFirstCell = 6;

    /// <summary>Constructs animation definitions for all silver pickup types.</summary>
    private static AnimationDef[] BuildSilverAnims()
    {
        (string Key, string? Note)[] types =
        [
            ("energy", "refills the energy meter"),
            ("spring_boots", null),
            ("rapid_fire", null),
            ("jetpack", null),
            ("time", "adds to the level timer"),
            ("super_dug", "the cape"),
            ("trail_mark", null),
        ];

        var anims = new AnimationDef[types.Length];
        for (int type = 0; type < anims.Length; type++)
        {
            var (key, note) = types[type];
            anims[type] = new AnimationDef(key, [SilverFirstCell + type], AnimMode.Pose,
                note is null ? $"type {type}" : $"type {type}; {note}",
                DisplayName: EntityCategories.SubtypeNameOf(0x01, (ushort)type)!);
        }
        return anims;
    }
}
