namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Player damage star effect animations (WO_BONK, sprite base 0xC0, seg3:0x845C).</summary>
    public static readonly CharacterAnimSet Hit = new("Hit", null,
    [
        // Labeled with the name the game registers the sheet under, which is also the only thing telling
        // this entry apart from its parent node.
        new("hit", Rng(0, 4), AnimMode.Once, "plays over the player while taking damage",
            DisplayName: "Bonk"),
    ],
        FixedSheet: "WO_BONK",
        FixedPalette: "LVL000.PAL",
        FixedLocation: "Dug (player)",
        SlicedSheet: true);
}
