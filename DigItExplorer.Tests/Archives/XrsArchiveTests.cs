using System.Security.Cryptography;
using DigItExplorer.Core.Archives;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies XRS archive container parsing against reference game files.</summary>
public class XrsArchiveTests
{
    /// <summary>The archives the copy in hand ships, in load order, which is fewer than six in the shareware.</summary>
    private static IEnumerable<string> ArchivePathsIn(string gameDir)
        => GameInstall.ArchiveNames.Select(name => Path.Combine(gameDir, name)).Where(File.Exists);

    /// <summary>Guards that all directory entries decode to valid printable filenames and read back declared byte counts.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Every_entry_decodes_to_a_usable_name_and_reads_back_in_full()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        var archives = ArchivePathsIn(gameDir).ToList();
        // Guards the guard: a folder holding no archive this build ships would pass the loop below.
        Assert.NotEmpty(archives);

        foreach (var archivePath in archives)
        {
            using var archive = XrsArchive.Open(archivePath);
            Assert.NotEmpty(archive.Entries);

            foreach (var entry in archive.Entries)
            {
                Assert.NotEmpty(entry.Name);
                Assert.All(entry.Name, c => Assert.InRange(c, ' ', '~'));
                Assert.Equal((int)entry.Length, archive.Read(entry).Length);
            }
        }
    }

    /// <summary>Build fingerprint test guarding entry counts and aggregate content SHA-256, against the row
    /// measured from whichever build the copy in hand is.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.MeasuredArchives)]
    public void Archives_hold_the_entries_the_build_in_hand_ships() // build fingerprint, not a guard: see the class summary
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(ArchiveFingerprints.TryIdentify(out var expected));

        int totalEntries = 0;
        var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var sha = SHA256.Create();

        foreach (var archivePath in ArchivePathsIn(gameDir))
        {
            using var archive = XrsArchive.Open(archivePath);
            foreach (var entry in archive.Entries)
            {
                totalEntries++;
                uniqueNames.Add(entry.Name);

                var bytes = archive.Read(entry);
                sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }
        }
        sha.TransformFinalBlock([], 0, 0);

        Assert.Equal(expected.Entries, totalEntries);
        Assert.Equal(expected.DistinctNames, uniqueNames.Count);
        Assert.Equal(expected.ContentSha256, Convert.ToHexString(sha.Hash!), ignoreCase: true);
    }
}
