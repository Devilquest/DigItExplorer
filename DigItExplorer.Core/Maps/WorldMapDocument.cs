using System.Linq;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Core.Maps;

/// <summary>Decoded world map containing raster layers, walk path, signs, and node metadata.</summary>
public sealed record WorldMapDocument(World World, int Width, int Height, VgaPalette Palette,
    WorldMapLayer? Sky, WorldMapLayer? Background, WorldMapLayer Front, WorldMapPath? Path, byte[]? SignSheet,
    IReadOnlyDictionary<int, SignType> NodeSigns)
{
    /// <summary>Reconstructs a world map document from archive resources.</summary>
    /// <param name="world">Target game world.</param>
    /// <param name="loadResource">Resolver function for raw archive resource bytes.</param>
    /// <param name="nodes">Node table data providing sign associations.</param>
    /// <returns>The reconstructed WorldMapDocument, or null if base plane is unavailable.</returns>
    public static WorldMapDocument? Load(World world, Func<string, byte[]?> loadResource, NodeTableData nodes)
    {
        var prefix = GameKnowledge.WorldMapPrefix(world);

        var ldMpf = loadResource($"{prefix}LD.MPF");
        if (ldMpf is null) return null;

        var palBytes = loadResource($"{prefix}.PAL");
        var baseImage = WorldMapCompositor.ComposeBase(ldMpf, palBytes);
        var palette = baseImage.Palette;

        var fMpf = loadResource($"{prefix}F.MPF");
        var front = fMpf is not null
            ? WorldMapCompositor.ComposeFront(fMpf)
            : WorldMapCompositor.FrontFromBase(baseImage); // the boss approach screen ships no F at all

        var skyBytes = loadResource($"{prefix}SKY.SPF");
        var sky = skyBytes is not null ? WorldMapCompositor.ComposeSky(skyBytes, front.Width) : null;

        // The masked variant (BKF+BKM) ships for caves/snow; water/underworld ship a single unmasked BK.
        var bkfBytes = loadResource($"{prefix}BKF.SPF");
        var bkmBytes = bkfBytes is not null ? loadResource($"{prefix}BKM.SPF") : null;
        var bkBytes = bkfBytes ?? loadResource($"{prefix}BK.SPF");
        var background = bkBytes is not null ? WorldMapCompositor.ComposeBackground(bkBytes, bkmBytes, front.Width) : null;

        var pMpf = loadResource($"{prefix}P.MPF");
        var path = pMpf is not null ? WorldMapCompositor.ComposePath(pMpf) : null;

        var sgnBytes = loadResource($"{prefix}SGN.SPF");
        var signSheet = sgnBytes is not null ? SheetImage.Read(sgnBytes).Frames[0] : null;

        var nodeSigns = nodes.Nodes(world).ToDictionary(n => n.Index, n => n.Sign);

        return new WorldMapDocument(world, front.Width, front.Height, palette, sky, background, front, path,
            signSheet, nodeSigns);
    }

    /// <summary>Gets the sign type for a node index, defaulting to SignType.Level if unmapped.</summary>
    public SignType SignOf(int nodeIndex) =>
        NodeSigns.TryGetValue(nodeIndex, out var sign) ? sign : SignType.Level;

    /// <summary>Gets the set of unique sign types present across all nodes in the map.</summary>
    public IReadOnlySet<SignType> SignTypesPresent()
    {
        if (Path is null || SignSheet is null) return new HashSet<SignType>();
        return Path.Nodes.Keys.Select(SignOf).ToHashSet();
    }
}
