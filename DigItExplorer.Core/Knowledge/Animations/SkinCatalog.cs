using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Knowledge.Animations;

/// <summary>A character world skin definition with sheet suffix, display label, and present animation flags.</summary>
public sealed record CharacterSkin(string Suffix, string Label, bool[] Present);

/// <summary>Enumerates available world skins, formats labels, and computes sprite cropping rectangles.</summary>
public sealed class SkinCatalog(Func<string, byte[]?> loadSheet, NodeTableData nodes, EntityNames? names = null)
{
    private readonly Dictionary<string, List<CharacterSkin>> _cache = new();

    // Caves/snow/underworld, no water, which uses the Aquatics instead of land enemies. Shared by the
    // grid-based enemy branch and the WorldSheet branch, so the one fact lives in one place.
    private static readonly string[] DefaultWorldSuffixes = ["00", "02", "03"];

    /// <summary>Returns the display name for a character from roster or name override.</summary>
    public string CharacterLabel(CharacterAnimSet set)
        => (set.RosterOrdinal is int ordinal ? names?[ordinal] : null) ?? set.DisplayName ?? set.Name;

    /// <summary>Returns the location or world label for a single-skin character.</summary>
    public string LocationLabel(CharacterAnimSet set) =>
        set.FixedLocationWorld is World world ? nodes.WorldName(world)
            : set.FixedLocation ?? CharacterLabel(set);

    /// <summary>Returns the formatted display name for an animation sequence.</summary>
    public string AnimLabel(CharacterAnimSet set, AnimationDef anim)
        => anim.VariantWorld is World world
            ? $"{PrettyAnimName(set.Name, anim)} ({nodes.WorldName(world)})"
            : PrettyAnimName(set.Name, anim);

    /// <summary>Computes or retrieves cached skins for an animation set.</summary>
    public IReadOnlyList<CharacterSkin> SkinsOf(CharacterAnimSet set)
    {
        if (!_cache.TryGetValue(set.Name, out var skins))
            _cache[set.Name] = skins = ComputeSkins(set);
        return skins;
    }

    /// <summary>Retrieves cached skins for a character without computing.</summary>
    public IReadOnlyList<CharacterSkin>? TryGetCached(string characterName)
        => _cache.TryGetValue(characterName, out var skins) ? skins : null;

    /// <summary>Computes which skins of a character this copy of the game ships with per-animation presence flags.</summary>
    private List<CharacterSkin> ComputeSkins(CharacterAnimSet set)
    {
        var skins = new List<CharacterSkin>();

        // One unsuffixed sheet and no per-world variant, so exactly one skin, labeled with where the
        // character is found rather than with its own name, which would read twice over in the info bar.
        if (set.FixedSheet is not null)
        {
            var bytes = loadSheet($"{set.FixedSheet}.SPF") ?? loadSheet($"{set.FixedSheet}.MPF");
            if (bytes is null) return skins;

            var present = PresentFlagsOf(set, null, bytes);
            skins.Add(new CharacterSkin("", LocationLabel(set), present));
            return skins;
        }

        // One skin per costume, keyed by its own filename digit rather than a world suffix. The
        // absent-animation rule applies unchanged, which is how a costume missing an animation surfaces
        // without a special case for it anywhere.
        if (set.SheetLetter is char letter)
        {
            foreach (var costume in PlayerCostumes.All)
            {
                string sheetName = $"DUG{costume.Code}{letter}.SPF";
                var bytes = loadSheet(sheetName);
                if (bytes is null) continue;
                bool unused = KnownResources.Unused.Contains(sheetName);

                var present = PresentFlagsOf(set, null, bytes);
                // A costume with no world of its own is labeled plainly: routing it through the
                // world-name path would either invent a world or repeat the unused tag the label carries.
                string skinLabel = costume.World is World w ? SkinLabel(set, w, unused) : "Unused costume";
                skins.Add(new CharacterSkin(costume.Code.ToString(), skinLabel, present));
            }
            return skins;
        }

        // World-suffixed sheets with no EnemyGrids entry. Bypasses the grid-based branch below the way the
        // fixed-sheet arm does, except that the file still varies one per world.
        if (set.WorldSheet is { } worldSheet)
        {
            foreach (var suffix in DefaultWorldSuffixes)
            {
                string stem = string.Format(worldSheet.SheetFormat, suffix);
                var bytes = loadSheet($"{stem}.SPF") ?? loadSheet($"{stem}.MPF");
                if (bytes is null) continue;
                bool unused = KnownResources.Unused.Contains($"{stem}.SPF") || KnownResources.Unused.Contains($"{stem}.MPF");

                var present = PresentFlagsOf(set, null, bytes);
                skins.Add(new CharacterSkin(suffix, SkinLabel(set, suffix, unused), present));
            }
            return skins;
        }

        if (set.Category is not byte cat || !EnemyGrids.TryGet(cat, out var grid)) return skins;

        foreach (var suffix in DefaultWorldSuffixes)
        {
            string spfName = $"{grid.Sheet}{suffix}.SPF", mpfName = $"{grid.Sheet}{suffix}.MPF";
            var bytes = loadSheet(spfName) ?? loadSheet(mpfName);
            if (bytes is null) continue;
            bool unused = KnownResources.Unused.Contains(spfName) || KnownResources.Unused.Contains(mpfName);

            var present = PresentFlagsOf(set, grid, bytes);
            skins.Add(new CharacterSkin(suffix, SkinLabel(set, suffix, unused), present));
        }
        return skins;
    }

    /// <summary>Per animation, whether at least one of its frames has art in this sheet: the shared core of
    /// every <see cref="ComputeSkins"/> branch, which differ only in how they find the bytes.</summary>
    /// <param name="grid">Null for the branches whose geometry comes from the set itself.</param>
    private static bool[] PresentFlagsOf(CharacterAnimSet set, EnemyGrid? grid, byte[] sheetBytes)
    {
        var pages = SheetImage.Read(sheetBytes).Frames;
        var rects = RectsOf(set, pages);
        var effectiveGrid = grid ?? FixedGrid(set, rects);
        var empty = new Dictionary<int, bool>();
        foreach (var f in set.Anims.SelectMany(a => a.Frames).Distinct())
            empty[f] = ResolveCell(rects, effectiveGrid, pages, f).IsEmptyFor(set.TransparentIndices);
        return set.Anims.Select(a => !a.Frames.All(f => empty[f])).ToArray();
    }

    /// <summary>Computes frame crop rectangles from sliced sheets or explicit definitions.</summary>
    public static IReadOnlyDictionary<int, FrameRect>? RectsOf(CharacterAnimSet set, IReadOnlyList<byte[]> pages)
    {
        if (!set.SlicedSheet) return set.Rects;

        var cells = new List<FrameRect>();
        for (int page = 0; page < pages.Count; page++)
        {
            byte separator = set.SliceSeparator ?? pages[page][0];
            var sliced = set.SliceBand is (int y1, int y2)
                ? SpriteSheetSlicer.Slice(pages[page], separator, y1, y2)
                : SpriteSheetSlicer.Slice(pages[page], separator);
            foreach (var cell in sliced)
                cells.Add(new FrameRect(page, cell.X, cell.Y, cell.W, cell.H));
        }

        var rects = new Dictionary<int, FrameRect>();
        foreach (var f in set.Anims.SelectMany(a => a.Frames).Distinct())
            if (f >= 0 && f < cells.Count) rects[f] = cells[f];
        return rects;
    }

    /// <summary>Extracts a sprite cell using explicit crop rects or uniform grid slicing.</summary>
    public static SpriteCell ResolveCell(IReadOnlyDictionary<int, FrameRect>? rects, EnemyGrid grid,
        IReadOnlyList<byte[]> pages, int frameIndex)
    {
        if (rects is not null && rects.TryGetValue(frameIndex, out var rect))
            return SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y, rect.X + rect.W - 1, rect.Y + rect.H - 1);
        return SpriteSheetSlicer.GridCell(pages, grid.Cols, grid.StrideX, grid.StrideY, grid.W, grid.H, frameIndex);
    }

    /// <summary>Calculates maximum cell dimensions across all frames in a set.</summary>
    public static (int W, int H) CellSize(IReadOnlyDictionary<int, FrameRect>? rects, EnemyGrid grid)
        => rects is { Count: > 0 }
            ? (rects.Values.Max(r => r.W), rects.Values.Max(r => r.H))
            : (grid.W, grid.H);

    /// <summary>Calculates maximum cell dimensions for a specific animation sequence.</summary>
    public static (int W, int H) CellSize(IReadOnlyDictionary<int, FrameRect>? rects, EnemyGrid grid,
        AnimationDef anim)
    {
        if (rects is null) return (grid.W, grid.H);
        var used = new HashSet<int>(anim.Frames);
        if (anim.Underlay is { } underlay) used.Add(underlay.Frame);

        int w = 0, h = 0;
        foreach (var (frame, rect) in rects)
            if (used.Contains(frame)) { w = Math.Max(w, rect.W); h = Math.Max(h, rect.H); }
        return w > 0 ? (w, h) : (grid.W, grid.H);
    }

    /// <summary>Creates a synthetic grid wrapper for fixed-sheet characters.</summary>
    public static EnemyGrid FixedGrid(CharacterAnimSet set, IReadOnlyDictionary<int, FrameRect>? rects)
    {
        var (w, h) = CellSize(rects, new EnemyGrid(set.FixedSheet!, 0, 0, 0, 0, 0));
        return new EnemyGrid(set.FixedSheet!, 0, 0, 0, w, h);
    }

    // The characters whose identity changes with the skin, a real reskin with its own name rather than a
    // recolor. The alternate name is a roster ordinal and not a literal, so it is read from the user's
    // executable like every other species name.
    private static readonly Dictionary<(string Character, World World), int> SkinAltNameOrdinals = new()
    {
        [("Draggo", World.Underworld)] = 36,
    };

    /// <summary>Generates a formatted skin display name from a world suffix.</summary>
    public string SkinLabel(CharacterAnimSet set, string suffix, bool unused)
        => SkinLabel(set, (World)(suffix[1] - '0'), unused);

    /// <summary>Generates a formatted skin display name for a given world.</summary>
    public string SkinLabel(CharacterAnimSet set, World world, bool unused)
    {
        var name = nodes.WorldName(world);
        if (SkinAltNameOrdinals.TryGetValue((set.Name, world), out int ordinal) && names?[ordinal] is string alt)
            name = $"{name} ({alt})";
        return unused ? $"{name} (unused)" : name;
    }

    // Sheet world-suffix → representative level palette (the sheets' own embedded palettes have
    // placeholder low indices, so the world's real level palette is used instead).
    public static readonly IReadOnlyDictionary<string, string> SuffixPal = new Dictionary<string, string>
    {
        [""] = "LVL200.PAL", ["00"] = "LVL000.PAL", ["02"] = "LVL400.PAL", ["03"] = "LVL600.PAL",
    };

    /// <summary>Converts an animation key name into a sentence-case display name.</summary>
    public static string PrettyAnimName(string characterKey, AnimationDef anim)
        => anim.DisplayName ?? PrettyAnimName(characterKey, anim.Name);

    /// <summary>Converts an animation key name into a sentence-case display name.</summary>
    public static string PrettyAnimName(string characterKey, string name)
    {
        if (name == "idle_wait_frames")
            return "Idle wait";
        if (name == "idle_wait_sim")
            return "Idle wait (simulated)";
        if (name == "swim_left_map")
            return "Swim left";
        if (name == "idle" && characterKey.StartsWith("Ghost ", StringComparison.Ordinal))
            return "Walk/idle";
        return char.ToUpperInvariant(name[0]) + name[1..].Replace('_', ' ');
    }
}
