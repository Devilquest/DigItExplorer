using System.Security.Cryptography;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>What one build's archives hold, measured once from a copy of it and guarded from then on.</summary>
/// <param name="Build">The build these numbers were measured from.</param>
/// <param name="Entries">Directory entries across every archive the build ships.</param>
/// <param name="DistinctNames">Names among those entries, which is fewer because some are in two archives.</param>
/// <param name="Sheets">Distinct <c>.SPF</c>, <c>.MPF</c> and <c>.ANI</c> names.</param>
/// <param name="SheetFrames">Frames those sheets decode to in total.</param>
/// <param name="ContentSha256">SHA-256 over every entry's bytes, archive by archive in load order.</param>
internal sealed record ArchiveFingerprint(
    string Build, int Entries, int DistinctNames, int Sheets, int SheetFrames, string ContentSha256);

/// <summary>One set of contents recorded for one of the game's archives.</summary>
/// <param name="Builds">The builds an archive with these contents belongs to.</param>
/// <param name="Lost">The entries these contents are damaged in, empty when they are a verified copy.</param>
internal sealed record KnownArchive(string FileName, string Sha256, string[] Builds, string[] Lost);

/// <summary>The archives this project recognizes by their own hash, and what that says about a copy.</summary>
internal static class ArchiveFingerprints
{
    private const string Full = "full", Manaccom = "manaccom", Shareware = "shareware";

    private static readonly ArchiveFingerprint[] Measured =
    [
        new(Full, 782, 738, 419, 3821, "CC3AE696D43CFAFA778F350222CC8E6DD64A3ECA71A8D6511D580B7666C3A2A3"),
        new(Manaccom, 781, 737, 418, 3820, "21F81B34F3493D928D482508E3C82249B952A6C8D9D9E2264B30012BD42B5237"),
        new(Shareware, 292, 248, 141, 509, "6ACF8A7A6CF7335802156B07897926F085DC2C3CD281D0247EDB914EB8505DB1"),
    ];

    private static readonly string[] BothFullEditions = [Full, Manaccom];

    private static readonly KnownArchive[] KnownArchives =
    [
        new("DIGIT0.XRS", "D31BE8576962C521E092049B1BDBED91887AC7ADBED0F13AD667A00242117E85", [Full], []),
        new("DIGIT0.XRS", "CC321B2604963F1FCEC81B75B77DB5B66619E98FD0963E41D21338D586BE144E", [Manaccom], []),
        new("DIGIT0.XRS", "6E431174EE598B1938A70442323CD959EB6ACEB40283F9EE1FF7E722C916A638", [Shareware], []),
        new("DIGIT0.XRS", "404550C7887844B350E4550832A42031E0361945E06970B937509EEA706EDE49", [Shareware],
            ["LVL032F.MPF", "MANLOGO.ANI"]),

        new("DIGIT1.XRS", "3ECF93043E8D4BC33E6AC66DB38558EF859E50705BF476C8F884A21DAFC12861", BothFullEditions, []),
        new("DIGIT1.XRS", "5D0AB379E28EBDFF5E308485CFF115209E28D05867153AED1B677517679699A1", [Shareware], []),
        new("DIGIT1.XRS", "A4C0478C4B5AE42C0BB6E3388FD40E8D822753462C6972888323A1647EFDE4A6", BothFullEditions,
            ["LVL020M.MPF", "LVL022F.MPF", "LVL023F.MPF"]),

        new("DIGIT2.XRS", "0CDA22FAFE842E6013018981632DADD0C29951CEE1782311E4F2A4A53E1509FF", BothFullEditions, []),
        new("DIGIT2.XRS", "67199C49AA35D59DB1BA358BC4057955F8D8F22497FC50AB5D218D477CCDA232", BothFullEditions,
            ["LVL280M.MPF", "LVL281M.MPF", "LVL282M.MPF", "LVL283M.MPF", "LVL283F.MPF", "LVL289M.MPF", "LVL289F.MPF",
             "LVL510F.MPF", "LVL510M.MPF", "LVL511F.MPF", "LVL511M.MPF", "LVL512F.MPF", "LVL512M.MPF",
             "LVL513F.MPF", "LVL513M.MPF", "LVL514F.MPF", "LVL514M.MPF", "LVL515F.MPF", "LVL515M.MPF"]),

        new("DIGIT3.XRS", "C7730FC551ED323DC83D69A6C4D80A0F4DC565399E98E0A3517F4CB0A417BE0C", BothFullEditions, []),
        new("DIGIT4.XRS", "E1E1A3322884E6F9E969044FA150DD54A09A55366E59D2A54893903DF8A10A3D", BothFullEditions, []),
        new("DIGITX.XRS", "706CC1135685C957B09DCF4934E7365C700F51879972D681412EE08F80D5B16D",
            [Full, Manaccom, Shareware], []),
    ];

    /// <summary>What the archives of the copy in hand turned out to be.</summary>
    /// <param name="Fingerprint">The measured row for this copy, or null when its archives are a set this project has never measured.</param>
    /// <param name="Lost">The entries this copy is recorded as having lost.</param>
    private sealed record Identity(ArchiveFingerprint? Fingerprint, IReadOnlySet<string> Lost);

    // Hashing the archives costs more than a test attribute should, and the answer cannot change mid-run,
    // so the whole discovery pass shares one reading.
    private static readonly Lazy<Identity> Copy = new(IdentifyCopy);

    /// <summary>The entries the copy in hand is recorded as having lost.</summary>
    public static IReadOnlySet<string> LostEntries => Copy.Value.Lost;

    /// <summary>The fingerprint of the copy the tests were pointed at, or none when its archives are a set
    /// this project has never measured.</summary>
    public static bool TryIdentify(out ArchiveFingerprint fingerprint)
    {
        fingerprint = Copy.Value.Fingerprint!;
        return fingerprint is not null;
    }

    /// <summary>The reason a test needing measured archives is skipped, or <c>null</c> when it has to run.</summary>
    public static string? SkipReason()
        => Copy.Value.Fingerprint is not null
            ? null
            : "the archives in this copy of the game are not a set this project has measured, so a difference"
              + " found here cannot be told from a damaged or unknown download";

    private static Identity IdentifyCopy()
    {
        if (!TestPaths.TryGetGameDir(out var gameDir, reportIfStrict: false)) return new(null, new HashSet<string>());

        var recognized = new List<KnownArchive>();
        foreach (var fileName in GameInstall.ArchiveNames)
        {
            var path = Path.Combine(gameDir, fileName);
            if (!File.Exists(path)) continue;

            if (HashOf(path) is not { } sha256) return new(null, new HashSet<string>());

            var known = KnownArchives.FirstOrDefault(archive =>
                string.Equals(archive.FileName, fileName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(archive.Sha256, sha256, StringComparison.OrdinalIgnoreCase));

            // One archive on no list makes the whole copy unmeasured: which build it is stops being decidable,
            // and so does whether anything beside it is damaged.
            if (known is null) return new(null, new HashSet<string>());
            recognized.Add(known);
        }

        var lost = new HashSet<string>(recognized.SelectMany(archive => archive.Lost), StringComparer.OrdinalIgnoreCase);
        return new(lost.Count == 0 ? MeasuredRowOf(recognized) : null, lost);
    }

    private static ArchiveFingerprint? MeasuredRowOf(List<KnownArchive> recognized)
    {
        if (recognized.Count == 0) return null;

        var shared = recognized
            .Select(archive => archive.Builds.ToHashSet(StringComparer.OrdinalIgnoreCase))
            .Aggregate((left, right) => [.. left.Intersect(right, StringComparer.OrdinalIgnoreCase)]);

        return shared.Count == 1
            ? Measured.FirstOrDefault(row => string.Equals(row.Build, shared.Single(), StringComparison.OrdinalIgnoreCase))
            : null;
    }

    private static string? HashOf(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
