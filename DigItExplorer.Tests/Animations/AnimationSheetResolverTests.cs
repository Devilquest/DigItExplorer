using System.IO;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards consistency between <see cref="AnimationSheetResolver"/> and <see cref="SkinCatalog"/> sheet shape resolutions.</summary>
public class AnimationSheetResolverTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_skin_the_catalog_lists_also_resolves_its_art()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(TestPaths.TryGetGameData(out var data));

        var catalog = new SkinCatalog(library.TryRead, data.Nodes, data.EntityNames);

        var unresolved = new List<string>();
        int resolved = 0;
        foreach (var set in AnimationTables.All.Values)
            foreach (var skin in catalog.SkinsOf(set))
            {
                var art = AnimationSheetResolver.Resolve(set, skin.Suffix, library.TryRead, out var failure);
                if (art is null) unresolved.Add($"{set.Name}/{skin.Suffix}: {failure}");
                else resolved++;
            }

        Assert.Empty(unresolved);
        Assert.True(resolved > 0, "the catalog listed no skin at all, so nothing was resolved");
    }

    [Fact]
    public void A_set_with_no_cataloged_grid_reports_that_rather_than_missing_files()
    {
        // Distinguishing the two matters: NoGrid is a gap in the decoded tables and MissingFiles is a gap in
        // the user's install, and the preview says so. A set with no sheet shape at all and no DLF category
        // can only be the former, whatever files are on disk.
        var orphan = new CharacterAnimSet("orphan", null, [new("pose", [0], AnimMode.Pose, "")]);

        var art = AnimationSheetResolver.Resolve(orphan, "00", _ => new byte[64_000], out var failure);

        Assert.Null(art);
        Assert.Equal(AnimationSheetFailure.NoGrid, failure);
    }

    [Fact]
    public void An_install_missing_the_sheet_reports_missing_files()
    {
        var art = AnimationSheetResolver.Resolve(AnimationTables.Plant, "", _ => null, out var failure);

        Assert.Null(art);
        Assert.Equal(AnimationSheetFailure.MissingFiles, failure);
    }
}
