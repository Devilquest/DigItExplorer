using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Core.Maps;

/// <summary>Decoded level map containing terrain, collision, entities, palette, and tune metadata.</summary>
public sealed record MapDocument(string Stem, TerrainImage Terrain, CollisionImage? Collision,
    IReadOnlyList<DlfRecord> Entities, VgaPalette? RealPalette, string? TuneFile)
{
    /// <summary>Resolved mechanism collider footprints for the level.</summary>
    internal IReadOnlyList<ColliderStamp> ColliderStamps { get; init; } = [];

    /// <summary>Whether any exit on the map leads into a bonus zone.</summary>
    public bool HasBonusDestinations { get; init; }

    /// <summary>Whether any exit on the map leads anywhere other than a bonus zone.</summary>
    public bool HasExitDestinations { get; init; }

    /// <summary>Unique mechanism category and type keys present on the collision plane.</summary>
    public IReadOnlyList<(byte Category, ushort? Type)> ColliderKeys
        => [.. ColliderStamps.Select(s => s.LayerKey).Distinct()];

    /// <summary>Reconstructs a level document from archive resources.</summary>
    /// <param name="stem">Level file stem (e.g., LVL000).</param>
    /// <param name="loadResource">Resolver function for raw archive resource bytes.</param>
    /// <returns>The reconstructed MapDocument, or null if terrain is unavailable.</returns>
    public static MapDocument? Load(string stem, Func<string, byte[]?> loadResource)
    {
        var fMpf = loadResource($"{stem}F.MPF");
        if (fMpf is null) return null;

        var dlf = loadResource($"{stem}.DLF");
        var palBytes = loadResource($"{stem}.PAL");
        var terrain = TerrainCompositor.Compose(fMpf, dlf, palBytes);
        var entities = dlf is null ? [] : DlfRecord.ReadAll(dlf);

        // Entity art needs the level's own .PAL: without it there is no reliable color for palette indices
        // 1-15, so a level missing one falls back to flat markers everywhere rather than to wrong colors.
        var realPalette = palBytes is { Length: >= 768 } ? VgaPalette.From6Bit(palBytes.AsSpan(0, 768)) : null;

        // The header's own tune selector, resolved to the file the game would play. A level whose DLF is
        // missing or too short to carry the word has no tune to name rather than a default one.
        var tuneFile = dlf is { Length: >= DlfHeader.SizeWithTune }
            ? GameKnowledge.TuneFile(DlfHeader.Read(dlf).TuneNo)
            : null;

        var mMpf = loadResource($"{stem}M.MPF");
        CollisionImage? collision = null;
        IReadOnlyList<ColliderStamp> stamps = [];
        if (mMpf is not null)
        {
            collision = CollisionCompositor.Compose(mMpf, dlf);
            entities = LandingSimulator.SynthesizeSpawns(entities, collision, stem);
            stamps = BuildColliderStamps(entities, stem, loadResource);
        }

        var destinations = DestinationKinds(entities, stem, loadResource);
        return new MapDocument(stem, terrain, collision, entities, realPalette, tuneFile)
        {
            ColliderStamps = stamps,
            HasBonusDestinations = destinations.Contains(true),
            HasExitDestinations = destinations.Contains(false),
        };
    }

    /// <summary>Tells, for every exit marker on the map, whether it leads into a bonus zone.</summary>
    private static HashSet<bool> DestinationKinds(IReadOnlyList<DlfRecord> entities, string stem,
        Func<string, byte[]?> loadResource)
    {
        bool exists(string name) => loadResource(name) is not null;
        return [.. entities
            .Select(e => ExitDestinations.DestinationSlotOf(e, stem, exists))
            .Where(slot => slot is not null)
            .Select(slot => ExitDestinations.LeadsToBonusZone(stem, slot!.Value, loadResource))];
    }

    /// <summary>Resolves collider footprint stamps from moving platforms and drains.</summary>
    private static IReadOnlyList<ColliderStamp> BuildColliderStamps(IReadOnlyList<DlfRecord> entities, string stem,
        Func<string, byte[]?> loadResource)
    {
        var stamps = new List<ColliderStamp>();

        if (entities.Any(e => e.Category == 0x04))
        {
            var woMvl = WorldSheets.ResolveWorldSheet("WO_MVL", stem, loadResource);
            if (woMvl is not null)
                stamps.AddRange(ColliderStamper.BuildMovingPlatformStamps(entities, woMvl));
        }

        if (entities.Any(e => e.Category == 0x5B))
        {
            var woDrain = loadResource("WO_DRAIN.SPF");
            if (woDrain is not null)
                stamps.AddRange(ColliderStamper.BuildDrainStamps(entities, woDrain));
        }

        return stamps;
    }
}
