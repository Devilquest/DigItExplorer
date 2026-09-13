using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Tests;

/// <summary>Guards the inventory of animation sets with compiled-in sprite geometry vs sliced sheets.</summary>
public class CompiledInGeometryTests
{
    private static readonly string[] CompiledInRects =
    [
        // WO_GHOST / WO_FISH / WO_BOSS: one shared sheet per family, cells at bare offsets.
        "Supreme Spurkasaur",
        "Ghost Slugger", "Ghost Draggo", "Ghost Rocker", "Ghost Pyrosaur",
        "Aqua Slugger", "Sea Draggo", "Rockerfish", "Sea Spurk", "Hopperfish", "Nirpies",
        // Nirpling packs two strip rows on Nirp's sheet, nothing like Nirp's own body grid.
        "Nirpling",
        // DUG*.SPF costume sheets: uniform grids, but the strides are stated only in code.
        "Dug", "DugCrouch", "DugWait", "DugDig", "DugSuper", "DugJetpack", "DugDirt", "DugSwim", "DugMap",
        // Loose effect and minigame sheets with no separators: WO_FBALL, GENERAL, GM2_SPIN.
        "Fireball", "Fireball (unused)", "General Effects", "Spin It!",
        // GM1_PCS/GM3_PCS: cells sit on their own navy square rather than behind a shared grid line, so the
        // sheet has no separator a slicer could walk.
        "Stop It! Pieces", "Stop It! Prize Icons", "Find It! Pieces", "Find It! Prize Icons",
        // GM0_TURN page 2: the file's three pages do not agree on a separator index, and the card grid the
        // slicer walks is already claimed by the Flip It! set, so this page's two award icons take their
        // corners from the game's own blit operands (seg3:0xB863/0xB88E) instead.
        "Flip It! Prize Icons",
        // WO_NRP: no separator lines; the egg's rectangle is registered geometry (seg3:0x8F59).
        "Nirp Egg",
        // GENERAL's HUD block: painted on an index-1 field, cells touching through index-0 pixels, so a
        // flood-fill on any index returns the whole block as one region, the rectangles exist only as
        // registration operands (the seg3:0x7C4A call sites).
        "HUD (General)",
    ];

    [Fact]
    public void Compiled_in_rect_tables_are_exactly_the_documented_inventory()
    {
        var actual = AnimationTables.All.Values
            .Where(s => s.Rects is not null)
            .Select(s => s.Name)
            .OrderBy(n => n, StringComparer.Ordinal);

        Assert.Equal(CompiledInRects.OrderBy(n => n, StringComparer.Ordinal), actual);
    }

    [Fact]
    public void No_set_both_slices_its_sheet_and_carries_a_rect_table()
    {
        // The two are alternatives, not layers: a set that did both would read the file and then ignore it.
        foreach (var set in AnimationTables.All.Values)
            Assert.False(set.SlicedSheet && set.Rects is not null, set.Name);
    }

    [Fact]
    public void Every_separator_packed_set_derives_its_geometry()
    {
        var sliced = AnimationTables.All.Values.Where(s => s.SlicedSheet).Select(s => s.Name);
        Assert.Equal(
            ["Hit", "Sparkles", "Flip It!", "Gold Goodies", "Silver Goodies", "Gems", "Plant", "Bubble",
                "Power-up Banners", "Status Icons", "Falling Rock", "Snowball"],
            sliced);
    }
}
