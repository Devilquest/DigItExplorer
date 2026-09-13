using System.Text;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Core.Knowledge;

/// <summary>The tune each level-select screen plays, read from the branch that picks it (seg2:0x120E).</summary>
public sealed class WorldMapMusic
{
    // push cs; push di, the pair that follows each mov di and confirms the string is being handed to a call.
    private static readonly byte[] PushCsDi = [0x0E, 0x57];

    private readonly Dictionary<World, string> _tunes = [];

    private WorldMapMusic() { }

    /// <summary>The bare tune name this world's screen plays, such as <c>TUNE1</c>.</summary>
    public string TuneName(World world) => _tunes[world];

    /// <summary>The music file this world's screen plays.</summary>
    public string TuneFile(World world) => GameKnowledge.TuneFile(TuneName(world));

    /// <summary>Parses the per-world tune names from the executable.</summary>
    internal static bool TryRead(ExeReader reader, out WorldMapMusic music)
    {
        music = null!;
        if (!reader.Matches(ExeLayout.WorldMapTuneWorldLoad, ExeLayout.MovAxWorld)) return false;

        var fallThrough = ExeLayout.WorldMapTuneFirstCase
            + (ExeLayout.WorldMapTuneCaseStride * ExeLayout.WorldMapTuneCaseCount);
        if (!TryReadTuneName(reader, fallThrough, out var defaultTune)) return false;

        var data = new WorldMapMusic();
        foreach (var world in GameKnowledge.Worlds) data._tunes[world] = defaultTune;

        for (int i = 0; i < ExeLayout.WorldMapTuneCaseCount; i++)
        {
            var site = ExeLayout.WorldMapTuneFirstCase + (i * ExeLayout.WorldMapTuneCaseStride);
            if (!reader.TryReadCmpAxImm16(site, out int world)) return false;
            if (!Enum.IsDefined((World)world)) return false;
            if (!TryReadTuneName(reader, site + 5, out var tune)) return false;

            data._tunes[(World)world] = tune;
        }

        music = data;
        return true;
    }

    private static bool TryReadTuneName(ExeReader reader, ExeAddress site, out string tune)
    {
        tune = "";
        if (!reader.TryReadPointer(site, out var namePointer)) return false;
        if (!reader.Matches(site + 3, PushCsDi)) return false;
        if (!reader.TryReadPascalString(namePointer, out var name) || name.Length == 0) return false;

        tune = Encoding.Latin1.GetString(name);
        return true;
    }
}
