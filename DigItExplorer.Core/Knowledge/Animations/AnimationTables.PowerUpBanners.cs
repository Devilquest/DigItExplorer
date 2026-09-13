namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Power-up banner caption strips (WO_G&amp;S cells 13-16 and 22).</summary>
    public static readonly CharacterAnimSet PowerUpBanners = new("Power-up Banners", null,
    [
        new("spring_boots", [13], AnimMode.Pose, "cell 13", DisplayName: "Spring Boots"),
        new("rapid_fire", [14], AnimMode.Pose, "cell 14", DisplayName: "Rapid Fire"),
        new("jet_pack", [15], AnimMode.Pose, "cell 15", DisplayName: "Jet Pack"),
        new("super_dug", [16], AnimMode.Pose, "cell 16; no Time banner exists", DisplayName: "Super Dug"),
        new("cheat_code", [22], AnimMode.Pose, "cell 22; the sheet's only cheat-code strip", DisplayName: "Cheat Code"),
    ],
        FixedSheet: "WO_G&S",
        FixedPalette: "LVL000.PAL",
        FixedLocation: "Goodies",
        SlicedSheet: true);
}
