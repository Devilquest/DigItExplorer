using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="SkinCatalog"/>'s per-character skin enumeration and labeling.</summary>
public class SkinCatalogTests
{
    private static (ResourceLibrary Library, SkinCatalog Catalog) OpenCatalog()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(TestPaths.TryGetGameData(out var data));
        var library = ResourceLibrary.Open(gameDir);
        return (library, new SkinCatalog(library.TryRead, data.Nodes, data.EntityNames));
    }

    /// <summary>Guards that Slugger excludes the missing snow skin and flags the duplicate underworld skin as unused.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Slugger_has_no_snow_skin_and_flags_its_underworld_duplicate_unused()
    {
        var opened = OpenCatalog();
        using var library = opened.Library;
        var catalog = opened.Catalog;
        var skins = catalog.SkinsOf(AnimationTables.All["Slugger"]);

        Assert.DoesNotContain(skins, s => s.Suffix == "02");
        var underworld = Assert.Single(skins, s => s.Suffix == "03");
        Assert.Contains("(unused)", underworld.Label);
    }

    /// <summary>Verifies that Draggo's underworld skin (suffix 03) is dynamically labeled with the Drakko reskin name.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Draggo_underworld_skin_is_labeled_drakko()
    {
        var opened = OpenCatalog();
        using var library = opened.Library;
        var catalog = opened.Catalog;
        var skins = catalog.SkinsOf(AnimationTables.All["Draggo"]);

        var underworld = Assert.Single(skins, s => s.Suffix == "03");
        Assert.Equal("Underworld (Drakko)", underworld.Label);
    }

    /// <summary>Verifies that Drakko's underworld skin flags the omitted scratch_head animation as not-present.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Draggo_underworld_skin_lacks_scratch_head()
    {
        var opened = OpenCatalog();
        using var library = opened.Library;
        var catalog = opened.Catalog;

        var set = AnimationTables.All["Draggo"];
        int scratchHeadIndex = set.Anims.ToList().FindIndex(a => a.Name == "scratch_head");
        Assert.True(scratchHeadIndex >= 0, "test assumption: Draggo has a scratch_head animation");
        var skins = catalog.SkinsOf(set);
        var underworld = Assert.Single(skins, s => s.Suffix == "03");
        Assert.False(underworld.Present[scratchHeadIndex]);

        // Sanity: the caves skin (which does ship the animation) has it present, proving the flag is
        // genuinely data-driven and not just always false.
        var caves = Assert.Single(skins, s => s.Suffix == "00");
        Assert.True(caves.Present[scratchHeadIndex]);
    }

    /// <summary>Verifies that Ghost sets expose a single skin labeled with their Spookstone location.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Ghost_has_a_single_skin_labeled_spookstone()
    {
        var opened = OpenCatalog();
        using var library = opened.Library;
        var catalog = opened.Catalog;
        var skins = catalog.SkinsOf(AnimationTables.All["Ghost Slugger"]);

        var skin = Assert.Single(skins);
        Assert.Equal("Spookstone", skin.Label);
    }

    /// <summary>Verifies that Dug's unused 5th costume is labeled explicitly as "Unused costume".</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Dug_costume_5_is_labeled_unused_costume()
    {
        var opened = OpenCatalog();
        using var library = opened.Library;
        var catalog = opened.Catalog;
        var skins = catalog.SkinsOf(AnimationTables.All["Dug"]);

        var costume5 = Assert.Single(skins, s => s.Suffix == "5");
        Assert.Equal("Unused costume", costume5.Label);
    }

    /// <summary>Verifies character-specific pretty animation name overrides for idle states.</summary>
    [Fact]
    public void PrettyAnimName_relabels_ghost_idle_but_not_dugs()
    {
        Assert.Equal("Walk/idle", SkinCatalog.PrettyAnimName("Ghost Slugger", "idle"));
        Assert.Equal("Idle", SkinCatalog.PrettyAnimName("Dug", "idle"));
    }

    /// <summary>Verifies that explicit animation display names take precedence over derived titles.</summary>
    [Fact]
    public void PrettyAnimName_prefers_an_animations_own_display_name()
    {
        var withDisplayName = new AnimationDef("reveal_dragon", [5], AnimMode.Once, "", DisplayName: "Reveal Draggo");
        var without = new AnimationDef("turn_up", [0], AnimMode.Once, "");

        Assert.Equal("Reveal Draggo", SkinCatalog.PrettyAnimName("Flip It!", withDisplayName));
        Assert.Equal("Reveal dragon", SkinCatalog.PrettyAnimName("Flip It!", withDisplayName.Name));
        Assert.Equal("Turn up", SkinCatalog.PrettyAnimName("Flip It!", without));
    }

    /// <summary>Guards that every fixed-sheet animation set defines a distinct location label or world.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_fixed_sheet_set_names_its_location()
    {
        var opened = OpenCatalog();
        using var library = opened.Library;
        var catalog = opened.Catalog;

        Assert.All(AnimationTables.All.Values.Where(s => s.FixedSheet is not null),
            s =>
            {
                Assert.True(s.FixedLocation is not null || s.FixedLocationWorld is not null,
                    $"{s.Name} names no location");
                Assert.NotEqual(catalog.CharacterLabel(s), catalog.LocationLabel(s));
            });
    }
}
