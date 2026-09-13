namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Gem and power-up twinkle glint animations (WO_GEMS, 9 cycles, 8 frames each).</summary>
    public static readonly CharacterAnimSet Sparkles = new("Sparkles", null,
        BuildSparkleAnims(),
        FixedSheet: "WO_GEMS",
        FixedPalette: "LVL000.PAL",
        FixedLocation: "Gems & power-ups",
        SlicedSheet: true);

    /// <summary>Constructs animation definitions for all sparkle color cycles.</summary>
    private static AnimationDef[] BuildSparkleAnims()
    {
        // Named by visual color, except gold and silver which match the pickups that spawn them.
        var names = new[]
        {
            "sparkle_pink", "sparkle_orange", "sparkle_purple", "sparkle_green", "sparkle_gold",
            "sparkle_red", "sparkle_silver", "sparkle_blue", "sparkle_bronze",
        };
        var anims = new AnimationDef[names.Length];
        for (int type = 0; type < names.Length; type++)
        {
            int first = 20 + type * 8;
            string note = names[type] switch
            {
                "sparkle_gold" => "type 4; always spawned by gold power-ups",
                "sparkle_silver" => "type 6; always spawned by silver power-ups",
                _ => $"type {type}",
            };
            anims[type] = new AnimationDef(names[type], Rng(first, first + 7), AnimMode.Loop, note,
                TicksPerStep: 2);
        }
        return anims;
    }
}
