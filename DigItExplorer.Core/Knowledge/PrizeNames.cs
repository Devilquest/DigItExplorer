using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Bonus prize identifiers in executable table order.</summary>
internal enum Prize { Draggo, ExtraDug, Gems25, Gems50, Energy }

/// <summary>Bonus prize short and long names loaded from <c>MAIN.EXE</c> string tables.</summary>
internal sealed class PrizeNames
{
    private readonly byte[][] _short;
    private readonly byte[][] _long;

    private PrizeNames(byte[][] shortNames, byte[][] longNames)
    {
        _short = shortNames;
        _long = longNames;
    }

    /// <summary>The short form of <paramref name="prize"/>: sized for a card/reel label.</summary>
    public byte[] Short(Prize prize) => _short[(int)prize];

    /// <summary>The long form of <paramref name="prize"/>: sized for a wider caption.</summary>
    public byte[] Long(Prize prize) => _long[(int)prize];

    /// <summary>Parses both short and long prize name tables from the executable.</summary>
    internal static bool TryRead(ExeReader reader, out PrizeNames names)
    {
        names = null!;
        if (!TryReadTable(reader, ExeLayout.PrizeNameShortTable, ExeLayout.PrizeNameShortStride, out var shortNames))
            return false;
        if (!TryReadTable(reader, ExeLayout.PrizeNameLongTable, ExeLayout.PrizeNameLongStride, out var longNames))
            return false;

        names = new PrizeNames(shortNames, longNames);
        return true;
    }

    // Fixed-stride records: offset is index * stride rather than a concatenated string chain.
    private static bool TryReadTable(ExeReader reader, ExeAddress start, int stride, out byte[][] names)
    {
        names = null!;
        var read = new byte[ExeLayout.PrizeNameCount][];

        for (int i = 0; i < read.Length; i++)
        {
            if (!reader.TryReadPascalString(start + i * stride, out var text) || text.Length == 0) return false;
            read[i] = text;
        }

        names = read;
        return true;
    }
}
