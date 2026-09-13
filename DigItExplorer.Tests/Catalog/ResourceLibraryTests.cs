using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Tests resource location and origin archive mapping in <see cref="ResourceLibrary"/>.</summary>
public class ResourceLibraryTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void ArchiveOf_names_the_xrs_a_known_resource_was_indexed_from()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var name = library.Names.First();
        var archive = library.ArchiveOf(name);

        Assert.NotNull(archive);
        Assert.EndsWith(".XRS", archive, StringComparison.OrdinalIgnoreCase);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void ArchiveOf_returns_null_for_a_name_the_install_does_not_have()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        Assert.Null(library.ArchiveOf("NOSUCHFILE.XXX"));
    }
}
