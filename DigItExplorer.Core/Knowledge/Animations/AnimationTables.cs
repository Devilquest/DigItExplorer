namespace DigItExplorer.Core.Knowledge.Animations;

/// <summary>Playback mode for an animation sequence.</summary>
public enum AnimMode
{
    /// <summary>Cycles while the state holds (walk etc.).</summary>
    Loop,
    /// <summary>Plays once, then transitions.</summary>
    Once,
    /// <summary>Single held frame rendered as a static image.</summary>
    Pose,
}

/// <summary>Definition of an animation sequence including frame indices, playback mode, and metadata.</summary>
/// <param name="Name">Animation lookup key name.</param>
/// <param name="Frames">Sequence of frame indices in playback order.</param>
/// <param name="Mode">Playback mode (Loop, Once, Pose).</param>
/// <param name="Note">Technical provenance and state machine notes.</param>
/// <param name="Seamless">True if loop wrap-around has no perceptible cut.</param>
/// <param name="TicksPerStep">Engine game ticks waited per step.</param>
/// <param name="DisplayName">Custom UI display name override, or null.</param>
/// <param name="VariantWorld">World context for world-specific animation variants, or null.</param>
/// <param name="Underlay">Base composite frame and pixel offset, or null.</param>
/// <param name="StepsAreState">True if frames represent discrete meter/count states rather than motion.</param>
public sealed record AnimationDef(string Name, IReadOnlyList<int> Frames, AnimMode Mode, string Note,
    bool Seamless = false, int TicksPerStep = 1, string? DisplayName = null, World? VariantWorld = null,
    (int Frame, int OffsetX, int OffsetY)? Underlay = null, bool StepsAreState = false);

/// <summary>Explicit crop rectangle on a sheet page for non-uniform sprites.</summary>
public readonly record struct FrameRect(int Page, int X, int Y, int W, int H);

/// <summary>Resolution template for world-suffixed sprite sheets without uniform grid entries.</summary>
/// <param name="SheetFormat">String format template for sheet filename (e.g. 'WO_RCK{0}').</param>
public sealed record WorldSheetSet(string SheetFormat);

/// <summary>Character animation set defining all animations, sheet resolution rules, and sprite slicing geometry.</summary>
public sealed record CharacterAnimSet(string Name, byte? Category, IReadOnlyList<AnimationDef> Anims,
    IReadOnlyDictionary<int, FrameRect>? Rects = null, string? FixedSheet = null, string? FixedPalette = null,
    string? FixedLocation = null, char? SheetLetter = null, IReadOnlySet<byte>? TransparentIndices = null,
    bool UseEmbeddedPalette = false, string? DisplayName = null, World? FixedLocationWorld = null,
    bool SlicedSheet = false, int? RosterOrdinal = null, byte? SliceSeparator = null,
    WorldSheetSet? WorldSheet = null, (int Y1, int Y2)? SliceBand = null);

/// <summary>Central registry and partial class root for all character animation tables.</summary>
public static partial class AnimationTables
{
    /// <summary>Milliseconds per animation step at 70 Hz VGA timing (3 vsyncs = ~42.86 ms).</summary>
    public const double DefaultFrameMs = 3000.0 / 70.0;

    /// <summary>Registry of all character animation sets keyed by character name.</summary>
    public static readonly IReadOnlyDictionary<string, CharacterAnimSet> All;

    static AnimationTables()
    {
        All = new Dictionary<string, CharacterAnimSet>
        {
            [Slugger.Name] = Slugger, [Draggo.Name] = Draggo, [Rocker.Name] = Rocker,
            [Spurk.Name] = Spurk, [Hopper.Name] = Hopper,
            [Nirp.Name] = Nirp, [Nirpling.Name] = Nirpling,
            [GhostSlugger.Name] = GhostSlugger, [GhostDraggo.Name] = GhostDraggo,
            [GhostRocker.Name] = GhostRocker, [GhostPyrosaur.Name] = GhostPyrosaur,
            [AquaSlugger.Name] = AquaSlugger, [SeaDraggo.Name] = SeaDraggo, [Rockerfish.Name] = Rockerfish,
            [SeaSpurk.Name] = SeaSpurk, [Hopperfish.Name] = Hopperfish, [Nirpies.Name] = Nirpies,
            [Troggi.Name] = Troggi, [PapaSpurk.Name] = PapaSpurk,
            [Grock.Name] = Grock, [Pyrosaur.Name] = Pyrosaur, [Boss.Name] = Boss,
            [Dug.Name] = Dug, [DugCrouch.Name] = DugCrouch, [DugWait.Name] = DugWait, [DugDig.Name] = DugDig,
            [DugSuper.Name] = DugSuper, [DugJetpack.Name] = DugJetpack, [DugSwim.Name] = DugSwim,
            [DugDirt.Name] = DugDirt, [DugMap.Name] = DugMap,
            [Fireball.Name] = Fireball, [FireballUnused.Name] = FireballUnused,
            [GeneralEffects.Name] = GeneralEffects, [Hit.Name] = Hit, [Sparkles.Name] = Sparkles,
            [FlipIt.Name] = FlipIt, [SpinIt.Name] = SpinIt,
            [GoldItems.Name] = GoldItems, [SilverItems.Name] = SilverItems, [Gems.Name] = Gems,
            [Plant.Name] = Plant, [Bubble.Name] = Bubble,
            [PowerUpBanners.Name] = PowerUpBanners, [StatusIcons.Name] = StatusIcons,
            [HudGeneral.Name] = HudGeneral,
            [StopItPieces.Name] = StopItPieces, [StopItPrizeIcons.Name] = StopItPrizeIcons,
            [FindItPieces.Name] = FindItPieces, [FindItPrizeIcons.Name] = FindItPrizeIcons,
            [FlipItPrizeIcons.Name] = FlipItPrizeIcons,
            [FallingRock.Name] = FallingRock, [NirpEgg.Name] = NirpEgg,
            [Snowball.Name] = Snowball,
        };
    }

    /// <summary>Inclusive range [a..b]: the shorthand most frame lists in this file are written with.</summary>
    private static int[] Rng(int a, int b)
    {
        var r = new int[b - a + 1];
        for (int i = 0; i < r.Length; i++) r[i] = a + i;
        return r;
    }

    private static int[] Rev(int[] frames)
    {
        Array.Reverse(frames);
        return frames;
    }
}
