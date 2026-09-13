using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Repository of standing knowledge notes attached to specific node identities.</summary>
internal static class NodeNotes
{
    /// <summary>Looks up the nearest standing note for the provided note keys.</summary>
    public static string? For(params NoteKey[] keys)
    {
        foreach (var key in keys)
            if (All.TryGetValue(key, out var note)) return note;
        return null;
    }

    private const string PieceSheetNote =
        "Two rows with two different jobs: the upper cells are drawn on the board itself, the lower pair " +
        "are the award icons shown in the prize column when a prize is granted. The extra life and the " +
        "energy award are missing from that row because they are not blits from this sheet. The game spawns " +
        "them as the level's own gold and silver pickups, drawn from WO_G&S.SPF.";

    /// <summary>All registered standing knowledge notes keyed by node identity.</summary>
    internal static readonly IReadOnlyDictionary<NoteKey, string> All = new Dictionary<NoteKey, string>
    {
        [NoteKey.Character(AnimationTables.FireballUnused)] =
            "Never drawn. Every Pyrosaur sheet carries its own copy of the fireball sprites, but nothing " +
            "loads it: the three shooters all draw the standalone sheet, WO_FBALL.SPF.",

        // No mention of the fireball copy these sheets also carry. That belongs to the node that shows it,
        // where a reader can see what is being talked about.
        [NoteKey.Character(AnimationTables.Pyrosaur)] =
            "Pyrosaur is placed in one world's levels only, so WO_RED03.MPF is the only one of its three " +
            "sheets the game ever loads.",

        [NoteKey.Skin(AnimationTables.Pyrosaur, "00")] =
            "WO_RED00.MPF is a byte-identical copy of WO_RED03.MPF, the sheet the one world Pyrosaur is " +
            "placed in loads. Nothing but the digit in its filename says which world it was meant for.",

        [NoteKey.Skin(AnimationTables.Pyrosaur, "02")] =
            "WO_RED02.MPF is art of its own, not a copy of the sheet the game loads: it was drawn for a " +
            "world this character never reaches. Pyrosaur is placed in one world's levels only, and " +
            "WO_RED03.MPF is the sheet that world loads.",

        [NoteKey.Skin(AnimationTables.Slugger, "03")] =
            "WO_SNL03.SPF is a byte-identical copy of WO_SNL00.SPF, kept for the one world Slugger never " +
            "reaches. Nothing but the digit in its filename says which world it was meant for.",

        [NoteKey.Skin(AnimationTables.PapaSpurk, "00")] =
            "WO_POP00.MPF is art of its own, not a copy of the sheet the game loads: it was drawn for a " +
            "world this character never reaches. Papa Spurk is placed in one world's levels only.",

        // The identity note lives on the underworld sheet alone, where the reader can see the different art
        // and the missing state. On the other worlds' sheets Drakko never appears, so there is nothing to
        // explain.
        [NoteKey.Skin(AnimationTables.Draggo, "03")] =
            "Drakko is not a species of its own: it is this same entity on this sheet, running the same " +
            "state machine. WO_DRG03.SPF carries fewer animations than the character's other sheets, though: " +
            "the scratch-head cells are empty, and the code skips that state outright here, so the " +
            "animation is absent rather than unused.",

        // Both sealed states are notable for the same reason: a state no level ever shows, which a reader
        // who has only played the game has no way to have met.
        [NoteKey.DigSpot(DigSpotState.BonusSealed)] =
            "A faint X leads down into a bonus zone. Coming back out boards it over, and it cannot be dug a " +
            "second time. Levels are built with the entrance open, so no level's own data selects this state.",

        [NoteKey.Drain(DrainState.Sealed)] =
            "A drain leads down to a bonus zone. Coming back out seals it, and it cannot be entered a " +
            "second time. Levels are built with the entrance open, so no level's own data selects this state.",

        [NoteKey.Character(AnimationTables.StopItPrizeIcons)] =
            "Two icons is the whole set, not a gap. A minigame also awards an extra life and an energy " +
            "refill, but the game grants those by spawning the level's own gold and silver pickups, drawn " +
            "from WO_G&S.SPF, so they are never blits from a board sheet.",

        [NoteKey.Character(AnimationTables.FlipItPrizeIcons)] =
            "Two icons is the whole set, not a gap. Flip It! also awards an extra life and an energy " +
            "refill, but the game grants those by spawning the level's own gold and silver pickups, drawn " +
            "from WO_G&S.SPF, so they are never blits from a board sheet.",

        [NoteKey.Character(AnimationTables.FindItPrizeIcons)] =
            "Find It! awards only a life, and the game grants it by spawning the level's own gold pickup, " +
            "drawn from WO_G&S.SPF, rather than blitting it from here. Both icons are still drawn on its " +
            "prize column, by the same code the other games use.",

        // The sheets keep the whole finding, overlap with the two notes above included: a reader who opens
        // one of these files directly has passed no prize-icon node on the way in.
        [NoteKey.File("GM1_PCS.SPF")] = PieceSheetNote,
        [NoteKey.File("GM3_PCS.SPF")] = PieceSheetNote,

        [NoteKey.File("LVL094M.MPF")] =
            "The level's collision plane is the first twelve of these blocks, which is all its grid calls " +
            "for. The other six are not collision data: they are terrain from another level, left behind " +
            "when the game was built, and the last three are byte-identical to the end of LVL090F.MPF. The " +
            "game places only the twelve. No other level file carries a surplus like this.",
    };
}
