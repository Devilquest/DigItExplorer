using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Cutscenes;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Knowledge.Info;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Menu;
using DigItExplorer.Core.Minigames;
using DigItExplorer.Core.Story;

namespace DigItExplorer.Tests;

/// <summary>Verifies that ResourceInfoBuilder resolves non-empty sections and valid file references for every node kind.</summary>
public class NodeKindInfoTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_level_in_the_install_resolves()
    {
        var library = Install(out var data, out _);
        using (library)
        {
            var stems = LevelStems(library).ToList();
            Assert.NotEmpty(stems);

            foreach (var stem in stems)
            {
                var map = MapDocument.Load(stem, library.TryRead);
                Assert.True(map is not null, $"{stem}F.MPF is in this install but would not load");
                AssertWellFormed(LevelInfoBuilder.Build(stem, map!, library, data.Nodes, data.EntityNames),
                    library);
            }
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_world_map_resolves()
    {
        var library = Install(out var data, out _);
        using (library)
        {
            foreach (var world in GameKnowledge.Worlds)
            {
                var map = WorldMapDocument.Load(world, library.TryRead, data.Nodes);
                Assert.True(map is not null, $"{GameKnowledge.WorldMapPrefix(world)}LD.MPF is missing from this install");
                AssertWellFormed(WorldMapInfoBuilder.Build(map!, library, data.Nodes, data.WorldMapMusic), library);
            }
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_animation_of_every_character_resolves()
    {
        var library = Install(out var data, out _);
        using (library)
        {
            var skins = new SkinCatalog(library.TryRead, data.Nodes, data.EntityNames);
            foreach (var set in AnimationTables.All.Values)
            {
                var suffixes = skins.SkinsOf(set);
                Assert.True(suffixes.Count > 0, $"{set.Name}: this install ships no skin for it");

                var suffix = suffixes[0].Suffix;
                var art = AnimationSheetResolver.Resolve(set, suffix, library.TryRead, out var failure);
                Assert.True(art is not null, $"{set.Name}/{suffix}: {failure}");

                foreach (var anim in set.Anims)
                    AssertWellFormed(AnimationInfoBuilder.Build(set, suffix, anim, art, skins, library), library);
            }
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_cutscene_piece_resolves()
    {
        var library = Install(out var data, out var font);
        using (library)
        {
            foreach (var piece in Enum.GetValues<CutscenePiece>())
            {
                // A piece only one build plays has no entry in the others, which is how the tree leaves it
                // out of those, so there is nothing here to resolve either.
                if (!data.Cutscenes.Pieces.TryGetValue(piece, out var info)) continue;

                var fileName = info.FileName;
                CutsceneClip clip;
                if (fileName is null)
                {
                    var palBytes = library.TryRead(data.Cutscenes.CardPaletteFile);
                    Assert.True(palBytes is { Length: >= 768 },
                        $"{data.Cutscenes.CardPaletteFile} is missing or too short in this install");
                    clip = CutsceneCompositor.BuildCard(font,
                        VgaPalette.From6Bit(palBytes!.AsSpan(0, 768)), data.Cutscenes);
                }
                else
                {
                    var aniBytes = library.TryRead(fileName);
                    Assert.True(aniBytes is not null, $"{fileName} is missing from this install");
                    clip = CutsceneCompositor.Build(piece, SheetImage.Read(aniBytes!), font,
                        data.Cutscenes);
                }

                AssertWellFormed(CutsceneInfoBuilder.Build(piece, data.Cutscenes, clip, library), library);
            }
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_note_scene_resolves()
    {
        var library = Install(out var data, out var font);
        using (library)
        {
            // The four per-world scenes, then the intro's own two, which belong to no world.
            foreach (var world in new[] { World.Caves, World.Water, World.Snow, World.Underworld })
                AssertNoteScene(library, data, font, $"R0{(int)world + 1}_SCRN.SPF", $"R0{(int)world + 1}_STRY.TXT",
                    world);
            AssertNoteScene(library, data, font, "INTRODRG.SPF", "INTRODRG.TXT", world: null);
            AssertNoteScene(library, data, font, "R00_SCRN.SPF", "R00_STRY.TXT", world: null);
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_ending_screen_resolves()
    {
        var library = Install(out var data, out _);
        using (library)
        {
            Assert.NotEmpty(data.EndSequence.Frames);

            for (int screen = 0; screen < data.EndSequence.Frames.Count; screen++)
                AssertWellFormed(EndSequenceInfoBuilder.Build(screen, data.EndSequence, library), library);
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_minigame_board_resolves()
    {
        var library = Install(out _, out _);
        using (library)
        {
            foreach (var board in Enum.GetValues<MinigameBoard>())
                AssertWellFormed(MinigameInfoBuilder.Build(board, library), library);
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_slab_stop_resolves()
    {
        var library = Install(out var data, out _);
        using (library)
        {
            foreach (int screen in new[] { SlabData.Instructions, SlabData.Credits })
            {
                int stops = data.Slab.SlabCount(screen);
                Assert.True(stops > 0, $"screen {screen} reports no stop to show");

                for (int stop = 0; stop < stops; stop++)
                    AssertWellFormed(SlabInfoBuilder.Build(screen, stop, data.Slab, library), library);
            }
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void The_main_menu_resolves()
    {
        var library = Install(out var data, out _);
        using (library)
        {
            var menu = MainMenuDocument.Load(library.TryRead);
            Assert.NotNull(menu);
            AssertWellFormed(MainMenuInfoBuilder.Build(menu, library, data.EntityNames), library);
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_platform_cell_resolves()
    {
        var library = Install(out var data, out _);
        using (library)
        {
            // Water and the boss screen ship no platform sheet at all, so the count is what says the three
            // that do were read rather than stepped over.
            int worldsWithPlatforms = 0;
            foreach (var world in GameKnowledge.Worlds)
            {
                int cells = PlatformCompositor.CellCount(library.TryRead, world);
                if (cells == 0) continue;
                worldsWithPlatforms++;

                for (int cell = 0; cell < cells; cell++)
                {
                    var composed = PlatformCompositor.Compose(library.TryRead, world, cell,
                        new SpriteRenderOptions(ShowGraphic: true, ShowCollision: true));
                    Assert.NotNull(composed);
                    var size = ((int, int)?)(composed!.Width, composed.Height);
                    AssertWellFormed(PlatformInfoBuilder.Build(world, cell, cells, size, library, data.Nodes),
                        library);
                }
            }

            Assert.Equal(3, worldsWithPlatforms);
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_drain_state_resolves()
    {
        var library = Install(out _, out _);
        using (library)
        {
            foreach (var state in (DrainState[])[DrainState.Open, DrainState.Sealed])
            {
                var composed = DrainCompositor.Compose(library.TryRead, state,
                    new SpriteRenderOptions(ShowGraphic: true, ShowCollision: true));
                Assert.NotNull(composed);
                var size = ((int, int)?)(composed!.Width, composed.Height);
                AssertWellFormed(DrainInfoBuilder.Build(state, size, library), library);
            }
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_dig_spot_state_resolves()
    {
        var library = Install(out _, out _);
        using (library)
        {
            foreach (var state in (DigSpotState[])[DigSpotState.Exit, DigSpotState.ExitUnderworld,
                         DigSpotState.Bonus, DigSpotState.BonusSealed])
            {
                var composed = DigSpotCompositor.Compose(library.TryRead, state);
                Assert.NotNull(composed);
                var size = ((int, int)?)(composed!.Width, composed.Height);
                AssertWellFormed(DigSpotInfoBuilder.Build(state, size, library), library);
            }
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void The_exit_sign_resolves()
    {
        var library = Install(out var data, out _);
        using (library)
        {
            var composed = ExitSignCompositor.Compose(library.TryRead);
            Assert.NotNull(composed);
            var size = ((int, int)?)(composed!.Width, composed.Height);
            AssertWellFormed(ExitSignInfoBuilder.Build(size, library, data.Nodes), library);
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_world_map_sign_resolves()
    {
        var library = Install(out var data, out _);
        using (library)
        {
            foreach (var world in new[] { World.Caves, World.Water, World.Snow, World.Underworld })
            {
                var sheet = WorldMapSignCompositor.Sheet(world);
                Assert.True(library.Contains(sheet), $"{sheet} is missing from this install");
                foreach (var type in Enum.GetValues<SignType>())
                {
                    var composed = WorldMapSignCompositor.Compose(library.TryRead, world, type);
                    Assert.NotNull(composed);
                    var size = ((int, int)?)(composed!.Width, composed.Height);
                    AssertWellFormed(WorldMapSignInfoBuilder.Build(world, type, size, library, data.Nodes),
                        library);
                }
            }
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_raw_file_resolves_to_a_source_section()
    {
        var library = Install(out _, out _);
        using (library)
        {
            Assert.NotEmpty(library.Names);

            foreach (var name in library.Names)
            {
                var info = ResourceInfoBuilder.Build(name, library);
                AssertWellFormed(info, library);
                // A raw file is the honest minimum: one section, and it is where the bytes came from.
                Assert.Equal("Source", Assert.Single(info.Sections).Title);
            }
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Raw_sheet_resolves_contents_with_dimensions_and_frames()
    {
        var library = Install(out _, out _);
        using (library)
        {
            var sheetName = library.Names.First(n => n.EndsWith(".SPF", StringComparison.OrdinalIgnoreCase));
            var info = ResourceInfoBuilder.BuildSheet(sheetName, library, frameCount: 12);
            AssertWellFormed(info, library);

            var contents = Assert.Single(info.Sections, s => s.Title == "Contents");
            Assert.Contains(contents.Rows, r => r.Label == "Dimensions" && r.Value == "320×200 px");
            Assert.Contains(contents.Rows, r => r.Label == "Frames" && r.Value == "12");
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void A_files_own_row_leads_to_it_everywhere_but_on_the_node_that_is_the_file()
    {
        var library = Install(out _, out _);
        using (library)
        {
            var name = library.Names.First();

            var elsewhere = ResourceInfoBuilder.Build(name, library);
            var itself = ResourceInfoBuilder.Build(name, library, sourceLeadsToItself: true);

            Assert.Equal(name, elsewhere.Sections.Single().Rows.Single().LabelFile);
            Assert.Null(itself.Sections.Single().Rows.Single().LabelFile);
        }
    }

    private static void AssertNoteScene(ResourceLibrary library, GameData data, GameFont font,
        string backgroundFile, string textFile, World? world)
    {
        var backgroundBytes = library.TryRead(backgroundFile);
        var textBytes = library.TryRead(textFile);
        Assert.True(backgroundBytes is not null, $"{backgroundFile} is missing from this install");
        Assert.True(textBytes is not null, $"{textFile} is missing from this install");

        var background = SheetImage.Read(backgroundBytes!);
        var scene = world is { } w
            ? StoryReconstructor.Build(font, background, textBytes!, w)
            : StoryReconstructor.Build(font, background, textBytes!, 1);

        AssertWellFormed(StoryInfoBuilder.Build(world is { } w2 ? data.Nodes.WorldName(w2) : null,
            backgroundFile, textFile, scene, library), library);
    }

    private static void AssertWellFormed(NodeInfo info, ResourceLibrary library)
    {
        Assert.NotEmpty(info.Sections);
        Assert.All(info.Sections, s => Assert.NotEmpty(s.Rows));
        // The Source section is the only one built from files rather than from knowledge, so it is missing
        // exactly when the install ships none of the files the node names.
        Assert.Contains(info.Sections, s => s.Title == "Source");

        // A music row is a pointer to the Music branch of the tree, so it has to spell its target the way
        // that branch does: same section wherever it appears, and a name the install actually holds. The
        // game names some of its tunes without the extension it loads them by, which reads as a plausible
        // file name while pointing at nothing.
        foreach (var section in info.Sections)
            foreach (var row in section.Rows.Where(r => r.Label == "Music"))
            {
                Assert.Equal("Contents", section.Title);
                Assert.True(library.Contains(row.Value), $"{row.Value} is not in this install");
            }

        // A row leads to a file by naming it, and a name the install does not hold leads nowhere.
        foreach (var row in info.Sections.SelectMany(s => s.Rows))
            foreach (var file in new[] { row.LabelFile, row.ValueFile }.OfType<string>())
                Assert.True(library.Contains(file), $"{file} is linked but is not in this install");
    }

    private static IEnumerable<string> LevelStems(ResourceLibrary library)
        => library.Names
            .Where(n => n.EndsWith("F.MPF", StringComparison.OrdinalIgnoreCase))
            .Select(GameKnowledge.LevelStem)
            .OfType<string>()
            .Distinct();

    private static ResourceLibrary Install(out GameData data, out GameFont font)
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(GameData.TryLoad(gameDir, out data));
        font = GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));
        return ResourceLibrary.Open(gameDir);
    }
}
