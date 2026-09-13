namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Caves plant decoration variants (WO_PLANT, category 0x5C).</summary>
    public static readonly CharacterAnimSet Plant = new("Plant", null,
        BuildPlantAnims(),
        FixedSheet: "WO_PLANT",
        FixedPalette: "LVL000.PAL",
        FixedLocationWorld: World.Caves,
        SlicedSheet: true,
        SliceSeparator: 224);

    /// <summary>Constructs animation definitions for all plant decoration types.</summary>
    private static AnimationDef[] BuildPlantAnims()
    {
        var anims = new AnimationDef[9];
        for (int type = 0; type < anims.Length; type++)
        {
            string name = EntityCategories.SubtypeNameOf(0x5C, (ushort)type)!;
            anims[type] = new AnimationDef($"plant_{type}", [type], AnimMode.Pose, $"type {type}",
                DisplayName: name);
        }
        return anims;
    }
}
