using System.Text.Json;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Locates the user's copy of the game (<c>DIGIT/</c>) for verification tests, searching no further than this repository.</summary>
internal static class TestPaths
{
    private const string ConfigFileName = "digit.tests.local.json";
    private const string SolutionFileName = "DigItExplorer.slnx";

    /// <summary>Whether a copy of the game can be located, without failing under <c>DIGIT_TESTS_STRICT</c>.</summary>
    internal static bool HasGameDir() => TryGetGameDir(out _, reportIfStrict: false);

    /// <summary>The reason a game-dependent test is skipped, or <c>null</c> when it has to run.</summary>
    internal static string? GameMissingSkipReason() => SkipReason(HasGameDir(), IsStrict);

    /// <summary>Under <c>DIGIT_TESTS_STRICT</c> a missing copy of the game is a failure, so the test runs and fails rather than reporting itself skipped.</summary>
    internal static string? SkipReason(bool gameFound, bool strict)
        => gameFound || strict ? null : "no game folder found; set DIGIT_GAME_DIR, add digit.tests.local.json, or place DIGIT/ inside the repository";

    /// <summary>The archives a copy has to ship for the worlds past the first to be in it.</summary>
    private static readonly string[] WorldArchivesPastTheFirst = ["DIGIT2.XRS", "DIGIT3.XRS", "DIGIT4.XRS"];

    /// <summary>The reason a test needing <paramref name="needs"/> is skipped, or <c>null</c> when it has to run.</summary>
    /// <param name="intact">Entries the test reads, which a copy recorded as having lost any of them cannot run it.</param>
    internal static string? SkipReasonFor(GameNeeds needs, IReadOnlyList<string> intact)
    {
        if (IsStrict) return null;
        if (SkipReason(HasGameDir(), strict: false) is { } missing) return missing;

        if (needs.HasFlag(GameNeeds.SupportedBuild) && !ReadsAsASupportedBuild.Value)
            return "MAIN.EXE in this copy of the game is not a build whose addresses this application knows";

        if (needs.HasFlag(GameNeeds.FullResourceSet) && !ShipsTheWholeGame.Value)
            return "this copy of the game ships one world, so the resources this test reads are not in it";

        if (needs.HasFlag(GameNeeds.MeasuredArchives) && ArchiveFingerprints.SkipReason() is { } unmeasured)
            return unmeasured;

        var lost = intact.Where(ArchiveFingerprints.LostEntries.Contains).ToList();
        return lost.Count == 0
            ? null
            : $"this copy of the game is recorded as having lost {string.Join(", ", lost)}";
    }

    // Both readings open files, and neither answer can change mid-run, so the whole discovery pass shares one.
    private static readonly Lazy<bool> ReadsAsASupportedBuild = new(
        () => TryGetGameDir(out var gameDir, reportIfStrict: false) && GameData.TryLoad(gameDir, out _));

    private static readonly Lazy<bool> ShipsTheWholeGame = new(
        () => TryGetGameDir(out var gameDir, reportIfStrict: false)
              && WorldArchivesPastTheFirst.All(name => File.Exists(Path.Combine(gameDir, name))));

    /// <summary>Resolves the game directory from <c>DIGIT_GAME_DIR</c>, then the local config file, then a <c>DIGIT/</c> folder in the search scope.</summary>
    public static bool TryGetGameDir(out string gameDir) => TryGetGameDir(out gameDir, reportIfStrict: true);

    internal static bool TryGetGameDir(out string gameDir, bool reportIfStrict)
    {
        var envDir = Environment.GetEnvironmentVariable("DIGIT_GAME_DIR");
        if (!string.IsNullOrEmpty(envDir) && IsGameDir(envDir))
        {
            gameDir = envDir;
            return true;
        }

        if (TryGetConfiguredGameDir(out var configuredDir) && IsGameDir(configuredDir))
        {
            gameDir = configuredDir;
            return true;
        }

        foreach (var dir in SearchScope())
        {
            var candidate = Path.Combine(dir.FullName, "DIGIT");
            if (IsGameDir(candidate))
            {
                gameDir = candidate;
                return true;
            }
        }

        gameDir = "";
        if (reportIfStrict) FailIfStrict("no game install found (checked DIGIT_GAME_DIR, " + ConfigFileName + ", and DIGIT/ inside the repository)");
        return false;
    }

    /// <summary>The test binary's directory and its parents up to the repository root, or none when the binary sits outside the repository.</summary>
    internal static IReadOnlyList<DirectoryInfo> SearchScope()
    {
        // Bounded at the repository root so a clone never reads the folders above it.
        var scope = new List<DirectoryInfo>();
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            scope.Add(dir);
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
                return scope;
        }
        return [];
    }

    private static bool IsGameDir(string candidate)
        => Directory.Exists(candidate) && Directory.EnumerateFiles(candidate, "*.XRS").Any();

    /// <summary>Reads <c>gameDir</c> out of a <c>digit.tests.local.json</c> in the search scope, resolving a relative value against that file's own folder.</summary>
    private static bool TryGetConfiguredGameDir(out string gameDir)
    {
        foreach (var dir in SearchScope())
        {
            var candidate = Path.Combine(dir.FullName, ConfigFileName);
            if (!File.Exists(candidate))
                continue;

            // This file is hand-edited from the .example, so malformed JSON is a realistic mistake:
            // fall through to the remaining resolution steps instead of throwing an opaque
            // JsonException out of every game-dependent test.
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(File.ReadAllText(candidate));
            }
            catch (JsonException)
            {
                continue;
            }

            using (doc)
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("gameDir", out var value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    var configured = value.GetString() ?? "";
                    gameDir = configured.Length == 0 ? "" : Path.GetFullPath(Path.Combine(dir.FullName, configured));
                    return gameDir.Length != 0;
                }
            }
        }
        gameDir = "";
        return false;
    }

    /// <summary>Loads <see cref="GameData"/> from the located game executable.</summary>
    public static bool TryGetGameData(out GameData data)
    {
        data = null!;
        if (!TryGetGameDir(out var gameDir)) return false;

        Assert.True(GameData.TryLoad(gameDir, out data),
            "MAIN.EXE was found but its game data could not be read at the mapped addresses.");
        return true;
    }

    /// <summary>Whether a missing prerequisite is a failure rather than a reason to skip.</summary>
    internal static bool IsStrict => Environment.GetEnvironmentVariable("DIGIT_TESTS_STRICT") == "1";

    private static void FailIfStrict(string reason)
    {
        if (IsStrict)
            throw new InvalidOperationException($"DIGIT_TESTS_STRICT=1 is set but {reason}. Fix the path or unset DIGIT_TESTS_STRICT to allow skipping.");
    }
}
