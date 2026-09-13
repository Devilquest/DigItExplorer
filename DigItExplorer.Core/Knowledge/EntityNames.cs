using System.Text;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Enemy species names and subtitles read from the ending gallery roster in <c>MAIN.EXE</c>.</summary>
public sealed class EntityNames
{
    private readonly string[] _names;

    private EntityNames(string[] names) => _names = names;

    /// <summary>Entries read, counting the mock-Latin subtitles.</summary>
    public int Count => _names.Length;

    /// <summary>The entry at <paramref name="ordinal"/>, or null if the roster does not reach it.</summary>
    public string? this[int ordinal] => ordinal >= 0 && ordinal < _names.Length ? _names[ordinal] : null;

    /// <summary>Parses the 44-entry enemy roster from the executable.</summary>
    internal static bool TryRead(ExeReader reader, out EntityNames names)
    {
        names = null!;
        var read = new string[ExeLayout.EnemyNameCount];

        var cursor = ExeLayout.EnemyNameTable;
        for (int i = 0; i < read.Length; i++)
        {
            if (!reader.TryReadPascalString(cursor, out var text) || text.Length == 0) return false;
            read[i] = Encoding.Latin1.GetString(text);
            cursor += 1 + text.Length;
        }

        names = new EntityNames(read);
        return true;
    }
}
