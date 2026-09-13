namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Nirp egg drop hazard animations (WO_NRP{suffix}, sprite base 650).</summary>
    public static readonly CharacterAnimSet NirpEgg = new("Nirp Egg", null,
    [
        new("egg", [0], AnimMode.Pose, "base 650 id 0"),
    ],
        Rects: new Dictionary<int, FrameRect> { [0] = new FrameRect(0, 0, 168, 13, 7) },
        WorldSheet: new WorldSheetSet("WO_NRP{0}"));
}
