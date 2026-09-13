using System.IO;
using DigItExplorer.Core.Archives;

namespace DigItExplorer.Core.Catalog;

/// <summary>Resource catalog indexing entries across all loaded <c>.XRS</c> archives by name.</summary>
/// <remarks>If identical names exist across multiple archives, the first encountered entry takes priority.</remarks>
public sealed class ResourceLibrary : IDisposable
{
    private static readonly HashSet<string> RenderableExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".SPF", ".MPF", ".ANI" };

    private readonly List<XrsArchive> _archives;
    private readonly Dictionary<string, (XrsArchive Arc, XrsEntry Entry)> _byName;

    /// <summary>The game folder this library was opened from.</summary>
    public string GameDir { get; }

    /// <summary>Every indexed resource name, naturally sorted.</summary>
    public IReadOnlyList<string> Names { get; }

    private ResourceLibrary(string gameDir, List<XrsArchive> archives,
                            Dictionary<string, (XrsArchive, XrsEntry)> byName)
    {
        GameDir = gameDir;
        _archives = archives;
        _byName = byName;
        Names = byName.Keys.OrderBy(n => n, NaturalStringComparer.Instance).ToList();
    }

    /// <summary>Opens every archive present in <paramref name="gameDir"/> and indexes their entries.</summary>
    public static ResourceLibrary Open(string gameDir)
    {
        var archives = new List<XrsArchive>();
        var byName = new Dictionary<string, (XrsArchive, XrsEntry)>(StringComparer.OrdinalIgnoreCase);

        foreach (var archiveName in GameInstall.ArchiveNames)
        {
            var path = Path.Combine(gameDir, archiveName);
            if (!File.Exists(path)) continue;

            var archive = XrsArchive.Open(path);
            archives.Add(archive);
            foreach (var entry in archive.Entries)
                byName.TryAdd(entry.Name, (archive, entry));
        }

        return new ResourceLibrary(gameDir, archives, byName);
    }

    /// <summary>Determines whether the specified file name has an image-renderable extension (.SPF, .MPF, .ANI).</summary>
    public static bool IsRenderable(string name)
        => RenderableExtensions.Contains(Path.GetExtension(name));

    /// <summary>Reads a resource by name.</summary>
    /// <param name="name">The name of the resource to read.</param>
    /// <returns>The raw byte contents of the resource.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the resource does not exist in the catalog.</exception>
    public byte[] Read(string name)
        => _byName.TryGetValue(name, out var loc)
            ? loc.Arc.Read(loc.Entry)
            : throw new KeyNotFoundException(name);

    /// <summary>Reads a resource by name, or returns null if the game folder does not contain it.</summary>
    public byte[]? TryRead(string name)
        => _byName.TryGetValue(name, out var loc) ? loc.Arc.Read(loc.Entry) : null;

    /// <summary>Whether this game folder contains <paramref name="name"/>.</summary>
    public bool Contains(string name) => _byName.ContainsKey(name);

    /// <summary>Byte size of a resource, or 0 if the game folder does not contain it.</summary>
    public long SizeOf(string name)
        => _byName.TryGetValue(name, out var loc) ? loc.Entry.Length : 0;

    /// <summary>File name of the <c>.XRS</c> archive a resource was indexed from, or null if the game folder does
    /// not contain it.</summary>
    public string? ArchiveOf(string name)
        => _byName.TryGetValue(name, out var loc) ? Path.GetFileName(loc.Arc.Path) : null;

    /// <summary>Closes every underlying archive.</summary>
    public void Dispose()
    {
        foreach (var archive in _archives) archive.Dispose();
    }
}
