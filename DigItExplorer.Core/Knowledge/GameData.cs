using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Aggregates game data tables, level nodes, cutscenes, and captions loaded from <c>MAIN.EXE</c>.</summary>
public sealed class GameData
{
    private GameData(NodeTableData nodes, CutsceneData cutscenes, EndSequenceData endSequence,
        EntityNames entityNames, PrizeNames prizeNames, byte[] findItStartLabel, StopItLabels stopItLabels,
        SlabData slab, WorldMapMusic worldMapMusic)
    {
        Nodes = nodes;
        Cutscenes = cutscenes;
        EndSequence = endSequence;
        EntityNames = entityNames;
        PrizeNames = prizeNames;
        FindItStartLabel = findItStartLabel;
        StopItLabels = stopItLabels;
        Slab = slab;
        WorldMapMusic = worldMapMusic;
    }

    /// <summary>The world maps' nodes, their in-game names, and the world names derived from the gate labels.</summary>
    public NodeTableData Nodes { get; }

    /// <summary>The intro and title cutscenes' playback values.</summary>
    public CutsceneData Cutscenes { get; }

    /// <summary>The ending sequence's per-screen captions.</summary>
    public EndSequenceData EndSequence { get; }

    /// <summary>The enemy roster the end-sequence gallery names each species by.</summary>
    public EntityNames EntityNames { get; }

    /// <summary>The five bonus-prize names the minigames award.</summary>
    internal PrizeNames PrizeNames { get; }

    /// <summary>The "Start" label Find It! draws under each panel before the shuffle.</summary>
    public byte[] FindItStartLabel { get; }

    /// <summary>The labels and ink styles Stop It! draws under its three reels.</summary>
    public StopItLabels StopItLabels { get; }

    /// <summary>The Instructions/Credits slab screens' stop counts, canvas size, parallax backdrop and
    /// counter-template text.</summary>
    public SlabData Slab { get; }

    /// <summary>The tune each level-select screen plays.</summary>
    public WorldMapMusic WorldMapMusic { get; }

    /// <summary>Loads all executable data tables from the game directory.</summary>
    public static bool TryLoad(string gameDir, out GameData data)
    {
        data = null!;

        GameExecutable exe;
        try { exe = GameExecutable.Open(gameDir, ExeLayout.MainExe); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }

        return TryLoad(exe, out data);
    }

    /// <summary>Loads all executable data tables from an executable whose build is not yet known.</summary>
    internal static bool TryLoad(GameExecutable exe, out GameData data)
    {
        data = null!;
        return BuildLayout.TryRecognize(exe, out var layout) && TryLoad(exe, layout, out data);
    }

    /// <summary>Loads all executable data tables from the opened game executable.</summary>
    internal static bool TryLoad(GameExecutable exe, BuildLayout layout, out GameData data)
    {
        data = null!;
        var reader = new ExeReader(exe, layout);
        if (!NodeTableData.TryRead(reader, out var nodes)) return false;
        if (!CutsceneData.TryRead(reader, out var cutscenes)) return false;
        if (!EndSequenceData.TryRead(reader, out var endSequence)) return false;
        if (!EntityNames.TryRead(reader, out var entityNames)) return false;
        if (!PrizeNames.TryRead(reader, out var prizeNames)) return false;
        if (!reader.TryReadPascalString(ExeLayout.FindItStartLabel, out var findItStartLabel) || findItStartLabel.Length == 0)
            return false;
        if (!StopItLabels.TryRead(reader, out var stopItLabels)) return false;
        if (!SlabData.TryRead(reader, out var slab)) return false;
        if (!WorldMapMusic.TryRead(reader, out var worldMapMusic)) return false;

        data = new GameData(nodes, cutscenes, endSequence, entityNames, prizeNames, findItStartLabel,
            stopItLabels, slab, worldMapMusic);
        return true;
    }
}
