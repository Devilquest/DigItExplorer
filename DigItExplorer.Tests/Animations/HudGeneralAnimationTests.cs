using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins HUD element frame rectangles and animation tables against reference game files.</summary>
public class HudGeneralAnimationTests
{
    // The dark/empty half of the time bar's own two-tone ramp; anything else is bar that is still full.
    private static readonly HashSet<byte> EmptyBarIndices = [133, 134];

    [Fact]
    public void Hud_table_matches_expected_shape()
    {
        var set = AnimationTables.HudGeneral;
        Assert.Equal("HUD (General)", set.Name);
        Assert.Equal("In-game HUD", set.DisplayName);
        Assert.Equal("GENERAL", set.FixedSheet);
        Assert.Equal("LVL000.PAL", set.FixedPalette);
        Assert.Equal("All levels", set.FixedLocation);
        Assert.Null(set.Category);
        Assert.False(set.SlicedSheet);
        Assert.NotNull(set.Rects);

        // 5 life-icon columns + the gem icon + 5 energy rows + 32 time levels + the time bar's frame.
        Assert.Equal(44, set.Rects!.Count);
        Assert.Equal(8, set.Anims.Count);
    }

    /// <summary>Guards that palette index 1 is configured as transparent for HUD elements.</summary>
    [Fact]
    public void Index_1_is_keyed_out_as_the_field_the_block_is_painted_on()
    {
        Assert.NotNull(AnimationTables.HudGeneral.TransparentIndices);
        Assert.Contains((byte)1, AnimationTables.HudGeneral.TransparentIndices!);
    }

    /// <summary>Verifies world assignments and unused status flags across life icon variants.</summary>
    [Fact]
    public void Life_icons_carry_their_world_and_only_the_unregistered_column_is_tagged()
    {
        var anims = AnimationTables.HudGeneral.Anims;
        Assert.Equal(
            [World.Caves, World.Water, World.Snow, null, World.Underworld],
            anims.Take(5).Select(a => a.VariantWorld));
        Assert.Equal(
            ["Life icon", "Life icon", "Life icon", "Life icon (unused)", "Life icon"],
            anims.Take(5).Select(a => a.DisplayName));
        Assert.All(anims.Skip(5), a => Assert.Null(a.VariantWorld));
    }

    /// <summary>Verifies that HUD meter and pickup labels are resolved dynamically from EntityCategories.</summary>
    [Fact]
    public void Meter_and_gem_labels_come_from_EntityCategories()
    {
        var anims = AnimationTables.HudGeneral.Anims;
        Assert.Equal(EntityCategories.LabelOf(0x02) + "s", anims[5].DisplayName);
        Assert.Equal(EntityCategories.SubtypeNameOf(0x01, 0), anims[6].DisplayName);
        Assert.Equal(EntityCategories.SubtypeNameOf(0x01, 4), anims[7].DisplayName);
        // And those are the same names the pickups themselves show, which is the point of sourcing them.
        Assert.Equal(
            SkinCatalog.PrettyAnimName("Silver Goodies", AnimationTables.SilverItems.Anims[0]),
            anims[6].DisplayName);
        Assert.Equal(
            SkinCatalog.PrettyAnimName("Silver Goodies", AnimationTables.SilverItems.Anims[4]),
            anims[7].DisplayName);
    }

    /// <summary>Verifies that energy and time meter frames are ordered from full to empty.</summary>
    [Fact]
    public void Both_meters_run_from_full_to_empty()
    {
        var anims = AnimationTables.HudGeneral.Anims;
        // Energy: DS:0x1298 starts at 4 (sprite 49, frame 10) and reaches 0 (sprite 45, frame 6).
        Assert.Equal([10, 9, 8, 7, 6], anims[6].Frames);
        // Time: DS:0x129A starts at 0xFFF, >> 7 = 31 (sprite 44, frame 42), down to 0 (sprite 13, frame 11).
        Assert.Equal(32, anims[7].Frames.Count);
        Assert.Equal(42, anims[7].Frames[0]);
        Assert.Equal(11, anims[7].Frames[^1]);
    }

    /// <summary>Guards that step timing displays are suppressed for state-driven meters.</summary>
    [Fact]
    public void Only_the_two_meters_suppress_step_timing()
    {
        var anims = AnimationTables.HudGeneral.Anims;
        Assert.True(anims[6].StepsAreState, "Energy");
        Assert.True(anims[7].StepsAreState, "Time");
        Assert.All(anims.Take(6), a => Assert.False(a.StepsAreState, a.Name));
    }

    /// <summary>Verifies that the time gauge fill is composed over the frame at the (5,2) pixel inset.</summary>
    [Fact]
    public void Time_bar_composes_its_fill_over_the_frame_at_the_games_own_offset()
    {
        var time = AnimationTables.HudGeneral.Anims[7];
        Assert.Equal((43, 5, 2), time.Underlay);
        Assert.DoesNotContain(43, AnimationTables.HudGeneral.Anims.SelectMany(a => a.Frames));

        // The inset is symmetric inside the frame, which is what "inside it" has to mean for a 32x7 fill in
        // a 42x11 frame: a cross-check on the two transcribed rects and the two draw positions at once.
        var rects = AnimationTables.HudGeneral.Rects!;
        Assert.Equal(2 * 5, rects[43].W - rects[11].W);
        Assert.Equal(2 * 2, rects[43].H - rects[11].H);
    }

    [Fact]
    public void Rects_match_the_transcribed_registration_operands()
    {
        var rects = AnimationTables.HudGeneral.Rects!;

        // sprite 0, seg3:0x7F89 (31x29 at 32 px pitch, the same cell for every column).
        for (int w = 0; w < 5; w++)
            Assert.Equal(new FrameRect(0, 32 * w, 93, 31, 29), rects[w]);
        // sprite 1, seg3:0x7FA2.
        Assert.Equal(new FrameRect(0, 43, 8, 23, 20), rects[5]);
        // sprites 45-49, seg3:0x7FED (39x9 at 10 px pitch, one column).
        for (int i = 0; i < 5; i++)
            Assert.Equal(new FrameRect(0, 132, 43 + 10 * i, 39, 9), rects[6 + i]);
        // sprites 13-44, seg3:0x8048, registered as 13 + (31 - (8j + k)), so frame 11 is the empty bar.
        Assert.Equal(new FrameRect(0, 99, 85, 32, 7), rects[11]);
        Assert.Equal(new FrameRect(0, 0, 29, 32, 7), rects[42]);
        // sprite 12, seg3:0x7FBB.
        Assert.Equal(new FrameRect(0, 0, 17, 42, 11), rects[43]);
    }

    /// <summary>Verifies that each HUD frame rectangle contains non-empty artwork in GENERAL.SPF.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Every_rect_lands_on_non_empty_art_in_the_sheet()
    {
        var page = ReadSheet();

        foreach (var (frame, rect) in AnimationTables.HudGeneral.Rects!)
        {
            var cell = SpriteSheetSlicer.RectCell(page, rect.X, rect.Y, rect.X + rect.W - 1, rect.Y + rect.H - 1);
            Assert.False(cell.IsEmptyFor(AnimationTables.HudGeneral.TransparentIndices),
                $"frame {frame} crops to nothing but the field it sits on");
        }
    }

    /// <summary>Verifies that the 32 time gauge steps decrease fill pixels monotonically.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Time_bar_steps_drain_the_gauge_monotonically()
    {
        var page = ReadSheet();

        var rects = AnimationTables.HudGeneral.Rects!;
        var filled = AnimationTables.HudGeneral.Anims[7].Frames.Select(frame =>
        {
            var r = rects[frame];
            int run = 0;
            // One row through the middle of the bar is enough: the fill is a solid column-wise run.
            for (int x = r.X; x < r.X + r.W; x++)
                if (!EmptyBarIndices.Contains(page[(r.Y + 3) * 320 + x])) run++;
            return run;
        }).ToList();

        Assert.Equal(32, filled[0]);
        Assert.Equal(0, filled[^1]);
        for (int i = 1; i < filled.Count; i++)
            Assert.True(filled[i] < filled[i - 1],
                $"step {i} leaves {filled[i]} px of bar, not less than step {i - 1}'s {filled[i - 1]}");
    }

    /// <summary>Same check as the time bar, in the artwork: each step has to lose exactly one lit
    /// sphere.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Energy_steps_lose_one_lit_sphere_each()
    {
        var page = ReadSheet();

        var rects = AnimationTables.HudGeneral.Rects!;
        var lit = AnimationTables.HudGeneral.Anims[6].Frames.Select(frame =>
        {
            var r = rects[frame];
            int count = 0;
            // Center of each of the four spheres, at the row's own 10 px pitch. A dim sphere and a lit one
            // are two different palette ramps, so any difference from the last row's dim value is "lit".
            for (int s = 0; s < 4; s++)
                if (page[(r.Y + 4) * 320 + r.X + 4 + s * 10] != page[(rects[6].Y + 4) * 320 + rects[6].X + 4])
                    count++;
            return count;
        }).ToList();

        Assert.Equal([4, 3, 2, 1, 0], lit);
    }

    /// <summary>The world in each label is the game's own word for it, not one written here.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Life_icon_labels_take_their_world_name_from_the_game()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var catalog = new SkinCatalog(_ => null, data.Nodes, data.EntityNames);
        var set = AnimationTables.HudGeneral;

        for (int i = 0; i < 5; i++)
        {
            var label = catalog.AnimLabel(set, set.Anims[i]);
            if (set.Anims[i].VariantWorld is World world)
                Assert.Equal($"Life icon ({data.Nodes.WorldName(world)})", label);
            else
                Assert.Equal("Life icon (unused)", label);
        }
    }

    /// <summary>Verifies that each HUD entry is sized to its own cells rather than the maximum set dimensions.</summary>
    [Fact]
    public void Each_entry_stages_at_its_own_cell_size()
    {
        var set = AnimationTables.HudGeneral;
        var grid = SkinCatalog.FixedGrid(set, set.Rects);

        Assert.Equal((31, 29), SkinCatalog.CellSize(set.Rects, grid, set.Anims[0]));
        Assert.Equal((23, 20), SkinCatalog.CellSize(set.Rects, grid, set.Anims[5]));
        Assert.Equal((39, 9), SkinCatalog.CellSize(set.Rects, grid, set.Anims[6]));
        // The composed time bar takes its underlay's size, not the 32x7 fill's.
        Assert.Equal((42, 11), SkinCatalog.CellSize(set.Rects, grid, set.Anims[7]));
        // The whole set's largest cell is neither of those, which is why it cannot be the stage size.
        Assert.Equal((42, 29), SkinCatalog.CellSize(set.Rects, grid));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Hud_resolves_a_single_skin_with_every_entry_present()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(TestPaths.TryGetGameData(out var data));

        var catalog = new SkinCatalog(library.TryRead, data.Nodes, data.EntityNames);

        var skins = catalog.SkinsOf(AnimationTables.HudGeneral);

        Assert.Single(skins);
        Assert.Equal("", skins[0].Suffix);
        Assert.Equal("All levels", skins[0].Label);
        Assert.All(skins[0].Present, Assert.True);
    }

    private static byte[] ReadSheet()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains("GENERAL.SPF"), "GENERAL.SPF is missing from this install");

        return SheetImage.Read(library.Read("GENERAL.SPF")).Frames[0];
    }
}
