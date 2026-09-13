using System.IO;
using System.Security.Cryptography;
using DigItExplorer.Core.Archives;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards duplicate name handling across archives against the reference install counts.</summary>
public class XrsDuplicateNameTests
{
    /// <summary>Every entry in the install, archive by archive, in the order the library reads them.</summary>
    private static List<(string Archive, XrsEntry Entry, XrsArchive Owner)> AllEntries(
        string gameDir, List<XrsArchive> opened)
    {
        var all = new List<(string, XrsEntry, XrsArchive)>();
        foreach (var archiveName in GameInstall.ArchiveNames)
        {
            var path = Path.Combine(gameDir, archiveName);
            if (!File.Exists(path)) continue;

            var archive = XrsArchive.Open(path);
            opened.Add(archive);
            foreach (var entry in archive.Entries) all.Add((archiveName, entry, archive));
        }
        return all;
    }

    private static string HashOf(XrsArchive archive, XrsEntry entry)
        => Convert.ToHexString(SHA256.HashData(archive.Read(entry)));

    /// <summary>Verifies all copies of duplicated archive entry names are byte-identical.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Copies_of_a_duplicated_name_are_byte_identical()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var opened = new List<XrsArchive>();
        try
        {
            var entries = AllEntries(gameDir, opened);
            Assert.True(entries.Count > 0, $"no archive of this install could be read in {gameDir}");

            var duplicated = entries
                .GroupBy(e => e.Entry.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();
            Assert.NotEmpty(duplicated);

            var differing = new List<string>();
            foreach (var group in duplicated)
            {
                var copies = group
                    .Select(e => (e.Archive, Hash: HashOf(e.Owner, e.Entry), e.Entry.Length))
                    .ToList();

                if (copies.Select(c => c.Hash).Distinct().Count() == 1) continue;

                differing.Add($"{group.Key}: " + string.Join(", ",
                    copies.Select(c => $"{c.Archive} ({c.Length} bytes, {c.Hash[..8]})")));
            }

            Assert.Empty(differing);
        }
        finally
        {
            foreach (var archive in opened) archive.Dispose();
        }
    }

    /// <summary>Guards the entry counts measured from whichever build the copy in hand is.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.MeasuredArchives)]
    public void The_install_holds_more_entries_than_distinct_names()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(ArchiveFingerprints.TryIdentify(out var expected));
        var opened = new List<XrsArchive>();
        try
        {
            var all = AllEntries(gameDir, opened);
            int distinct = all.Select(e => e.Entry.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count();

            Assert.Equal(expected.Entries, all.Count);
            Assert.Equal(expected.DistinctNames, distinct);
        }
        finally
        {
            foreach (var archive in opened) archive.Dispose();
        }
    }
}
