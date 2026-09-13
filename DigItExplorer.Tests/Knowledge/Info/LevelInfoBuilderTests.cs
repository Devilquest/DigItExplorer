using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Info;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Tests;

/// <summary>Verifies level metadata and diagnostic section assembly in <see cref="LevelInfoBuilder"/>.</summary>
public class LevelInfoBuilderTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void A_levels_source_section_reports_exactly_the_backing_files_actually_on_disk()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        var stem = FirstLevelStem(library);
        Assert.NotNull(stem);
        var map = MapDocument.Load(stem, library.TryRead);
        Assert.NotNull(map);

        var info = LevelInfoBuilder.Build(stem, map!, library, NodesOf(gameDir), null);

        var source = info.Sections.Single(s => s.Title == "Source");
        var expectedFiles = new[] { $"{stem}F.MPF", $"{stem}M.MPF", $"{stem}.DLF", $"{stem}.PAL" }
            .Where(f => library.TryRead(f) is not null)
            .ToList();
        Assert.Equal(expectedFiles.Count, source.Rows.Count);
        foreach (var file in expectedFiles)
            Assert.Contains(source.Rows, r => r.Label == file && r.LabelFile == file);

        Assert.DoesNotContain(source.Rows, r => r.Label == "Palette");
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void A_level_with_no_real_palette_states_the_fallback_instead_of_a_file_name()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        var stem = FirstLevelStem(library);
        Assert.NotNull(stem);
        var map = MapDocument.Load(stem, library.TryRead);
        Assert.NotNull(map);
        var noPalette = map! with { RealPalette = null };

        var info = LevelInfoBuilder.Build(stem, noPalette, library, NodesOf(gameDir), null);

        var palette = info.Sections.Single(s => s.Title == "Source").Rows.Single(r => r.Label == "Palette");
        Assert.Equal("Fallback (no .PAL in this copy of the game)", palette.Value);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void No_section_the_builder_returns_is_ever_empty()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        var stem = FirstLevelStem(library);
        Assert.NotNull(stem);
        var map = MapDocument.Load(stem, library.TryRead);
        Assert.NotNull(map);

        var info = LevelInfoBuilder.Build(stem, map!, library, NodesOf(gameDir), null);

        Assert.All(info.Sections, s => Assert.NotEmpty(s.Rows));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void A_level_with_no_node_table_to_name_it_falls_back_to_its_stem()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        var stem = FirstLevelStem(library);
        Assert.NotNull(stem);
        var map = MapDocument.Load(stem, library.TryRead);
        Assert.NotNull(map);

        var info = LevelInfoBuilder.Build(stem, map!, library, null, null);

        var identity = info.Sections.Single(s => s.Title == "Identity");
        Assert.Equal(stem, identity.Rows.Single(r => r.Label == "Name").Value);
        Assert.DoesNotContain(identity.Rows, r => r.Label == "World");
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void A_levels_music_is_the_tune_its_own_header_selects_and_the_install_ships()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        var stem = FirstLevelStem(library);
        Assert.NotNull(stem);
        var map = MapDocument.Load(stem, library.TryRead);
        Assert.NotNull(map);
        // Null only where the DLF is missing or too short to reach the header's tune word, which is a
        // damaged install rather than a level without music.
        Assert.NotNull(map!.TuneFile);

        var info = LevelInfoBuilder.Build(stem, map, library, NodesOf(gameDir), null);

        var music = info.Sections.Single(s => s.Title == "Contents").Rows.Single(r => r.Label == "Music");
        Assert.Equal(map.TuneFile, music.Value);
        // The header selects a tune by number; the file that number names has to be one this install has,
        // or the row is pointing at nothing.
        Assert.True(library.Contains(music.Value), $"{music.Value} is not in this install");
        Assert.Equal(music.Value, music.ValueFile);
    }

    private static NodeTableData NodesOf(string gameDir)
    {
        Assert.True(GameData.TryLoad(gameDir, out var data));
        return data.Nodes;
    }

    private static string? FirstLevelStem(ResourceLibrary library)
    {
        foreach (var name in library.Names)
        {
            if (!name.EndsWith("F.MPF", StringComparison.OrdinalIgnoreCase)) continue;
            var stem = GameKnowledge.LevelStem(name);
            if (stem is not null) return stem;
        }
        return null;
    }
}
