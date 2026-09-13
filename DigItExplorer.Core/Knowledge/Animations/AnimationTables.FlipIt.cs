namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Flip It! bonus minigame card animations (GM0_TURN).</summary>
    public static readonly CharacterAnimSet FlipIt = new("Flip It!", null,
    [
        new("turn_up", Rng(0, 4), AnimMode.Once, "face-down to side-edge"),
        new("turn_down", Rev(Rng(0, 4)), AnimMode.Once, "side-edge to face-down"),
        // Each prize is labeled as the game shows it rather than by the sheet's own ordering: the card
        // this table keys as "gold" is the extra-Dug prize.
        new("reveal_dragon", Rng(5, 9), AnimMode.Once, "side-edge to face-up", DisplayName: "Reveal Draggo"),
        new("hide_dragon", Rev(Rng(5, 9)), AnimMode.Once, "face-up to side-edge", DisplayName: "Hide Draggo"),
        new("reveal_gold", Rng(10, 14), AnimMode.Once, "side-edge to face-up", DisplayName: "Reveal Extra Dug"),
        new("hide_gold", Rev(Rng(10, 14)), AnimMode.Once, "face-up to side-edge", DisplayName: "Hide Extra Dug"),
        new("reveal_gem25", Rng(15, 19), AnimMode.Once, "side-edge to face-up", DisplayName: "Reveal 25 Gems"),
        new("hide_gem25", Rev(Rng(15, 19)), AnimMode.Once, "face-up to side-edge", DisplayName: "Hide 25 Gems"),
        new("reveal_gem50", Rng(20, 24), AnimMode.Once, "side-edge to face-up", DisplayName: "Reveal 50 Gems"),
        new("hide_gem50", Rev(Rng(20, 24)), AnimMode.Once, "face-up to side-edge", DisplayName: "Hide 50 Gems"),
        new("reveal_energy", Rng(25, 29), AnimMode.Once, "side-edge to face-up", DisplayName: "Reveal Energy"),
        new("hide_energy", Rev(Rng(25, 29)), AnimMode.Once, "face-up to side-edge", DisplayName: "Hide Energy"),
    ],
        FixedSheet: "GM0_TURN",
        FixedPalette: "GM0_PAL.PAL",
        FixedLocation: "Bonus minigame",
        SlicedSheet: true);
}
