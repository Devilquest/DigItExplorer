using DigItExplorer.Core.Archives;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="KnownResources"/>'s duplicate/unused-resource census against the archive.</summary>
public class KnownResourcesTests
{
    /// <summary>The census the table carries, held on a machine with no copy of the game on it.</summary>
    [Fact]
    public void Audio_duplicate_table_names_the_orphaned_jingles()
    {
        // The test below is three loops over this table, so an empty one would read the archives zero times
        // and pass.
        Assert.Equal(
            ["J_LOOP3.DAT", "J_BONUS.DAT", "J_SUPERM.DAT", "J_LOOP.DAT", "J_LOOP2.DAT"],
            KnownResources.AudioDuplicateOf.Keys);
    }

    /// <summary>Guards that orphaned audio track duplicates byte-match their active counterparts and match unused census rules.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Audio_duplicate_table_matches_archive_bytes()
    {
        foreach (var (orphan, twin) in KnownResources.AudioDuplicateOf)
        {
            Assert.Contains(orphan, KnownResources.Unused);
            Assert.DoesNotContain(twin, KnownResources.Unused);
        }

        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        var wanted = new Dictionary<string, byte[]?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (orphan, twin) in KnownResources.AudioDuplicateOf)
        {
            wanted[orphan] = null;
            wanted[twin] = null;
        }

        foreach (var archivePath in Directory.EnumerateFiles(gameDir, "*.XRS"))
        {
            using var archive = XrsArchive.Open(archivePath);
            foreach (var entry in archive.Entries)
                if (wanted.ContainsKey(entry.Name) && wanted[entry.Name] is null)
                    wanted[entry.Name] = archive.Read(entry);
        }

        foreach (var (orphan, twin) in KnownResources.AudioDuplicateOf)
        {
            Assert.NotNull(wanted[orphan]);
            Assert.NotNull(wanted[twin]);
            Assert.True(wanted[orphan]!.AsSpan().SequenceEqual(wanted[twin]!),
                $"{orphan} is not byte-identical to {twin}");
        }
    }
}
