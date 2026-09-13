namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Power-up HUD status icons (WO_G&amp;S cells 17-21, seg3:0x16F5).</summary>
    public static readonly CharacterAnimSet StatusIcons = new("Status Icons", null,
    [
        new("spring_boots", [17], AnimMode.Pose, "cell 17", DisplayName: "Spring Boots"),
        new("rapid_fire", [18], AnimMode.Pose, "cell 18", DisplayName: "Rapid Fire"),
        new("jet_pack", [19], AnimMode.Pose, "cell 19", DisplayName: "Jet Pack"),
        new("time", [20], AnimMode.Pose, "cell 20", DisplayName: "Time"),
        new("super_dug", [21], AnimMode.Pose, "cell 21", DisplayName: "Super Dug"),
    ],
        FixedSheet: "WO_G&S",
        FixedPalette: "LVL000.PAL",
        FixedLocation: "Goodies",
        SlicedSheet: true);
}
