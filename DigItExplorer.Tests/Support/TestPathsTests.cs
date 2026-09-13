namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="TestPaths"/> looks for the game inside this repository and nowhere above it.</summary>
public class TestPathsTests
{
    /// <summary>The last folder searched is the repository root, and every other one is inside it.</summary>
    [Fact]
    public void The_search_for_the_game_stops_at_the_repository_root()
    {
        var scope = TestPaths.SearchScope();

        Assert.NotEmpty(scope);

        var root = scope[^1];
        Assert.True(File.Exists(Path.Combine(root.FullName, "DigItExplorer.slnx")),
            $"the last folder searched should hold the solution file, and it was {root.FullName}");
        Assert.All(scope, dir =>
            Assert.StartsWith(root.FullName, dir.FullName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Strict mode asks for a failure, so it must never produce a skip instead.</summary>
    [Fact]
    public void Strict_mode_never_turns_a_missing_install_into_a_skip()
    {
        Assert.NotNull(TestPaths.SkipReason(gameFound: false, strict: false));
        Assert.Null(TestPaths.SkipReason(gameFound: false, strict: true));
        Assert.Null(TestPaths.SkipReason(gameFound: true, strict: false));
    }
}
