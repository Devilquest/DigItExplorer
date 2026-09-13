namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Bubble ring animations (WO_BUBLE, 3 sizes, sprite base 245).</summary>
    public static readonly CharacterAnimSet Bubble = new("Bubble", null,
    [
        new("bubble_small", [2], AnimMode.Pose, "sprite id 0; rightmost, smallest slice cell",
            DisplayName: "Bubble (small)"),
        new("bubble_medium", [1], AnimMode.Pose, "sprite id 1; middle slice cell",
            DisplayName: "Bubble (medium)"),
        new("bubble_large", [0], AnimMode.Pose, "sprite id 2; leftmost, largest slice cell",
            DisplayName: "Bubble (large)"),
    ],
        FixedSheet: "WO_BUBLE",
        FixedPalette: "LVL200.PAL",
        FixedLocationWorld: World.Water,
        SlicedSheet: true);
}
