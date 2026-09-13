using System.Text.RegularExpressions;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Knowledge.Info;

namespace DigItExplorer.Tests;

// Nothing here checks that a note names a character that exists: a key is built from the table entry
// itself, so a character that is gone takes the note's own line down with it at compile time.

/// <summary>Verifies diagnostic note lookup precedence and file reference integrity in <see cref="NodeNotes"/>.</summary>
public class NodeNotesTests
{
    [Fact]
    public void A_node_with_no_note_gets_none()
        => Assert.Null(NodeNotes.For(NoteKey.Character(AnimationTables.Rocker)));

    [Fact]
    public void The_nearest_key_wins()
    {
        var set = AnimationTables.Pyrosaur;

        var note = NodeNotes.For(NoteKey.Skin(set, "00"), NoteKey.Character(set));

        Assert.Equal(NodeNotes.All[NoteKey.Skin(set, "00")], note);
        Assert.NotEqual(NodeNotes.All[NoteKey.Character(set)], note);
    }

    [Fact]
    public void A_key_with_no_note_falls_through_to_the_next()
    {
        var set = AnimationTables.Pyrosaur;

        // The skin the game actually loads carries no note of its own, so the leaf reads the character's.
        var note = NodeNotes.For(NoteKey.Animation(set, "03", set.Anims[0]), NoteKey.Skin(set, "03"),
            NoteKey.Character(set));

        Assert.Equal(NodeNotes.All[NoteKey.Character(set)], note);
    }

    [Fact]
    public void A_file_note_is_found_whatever_case_the_name_is_in()
        => Assert.Equal(NodeNotes.For(NoteKey.File("GM1_PCS.SPF")), NodeNotes.For(NoteKey.File("gm1_pcs.spf")));

    [Fact]
    public void No_note_is_blank()
    {
        foreach (var (key, note) in NodeNotes.All)
            Assert.False(string.IsNullOrWhiteSpace(note), $"{key.Id} has no text");
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void A_noted_set_carries_its_note_through_to_the_built_info()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(GameData.TryLoad(gameDir, out var data));
        var skins = new SkinCatalog(library.TryRead, data.Nodes, data.EntityNames);
        var set = AnimationTables.FireballUnused;
        var art = AnimationSheetResolver.Resolve(set, "00", library.TryRead, out _);

        var info = AnimationInfoBuilder.Build(set, "00", set.Anims[0], art, skins, library);

        Assert.Equal(NodeNotes.All[NoteKey.Character(set)], info.Note);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void A_noted_skin_carries_its_own_note_rather_than_the_characters()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(GameData.TryLoad(gameDir, out var data));
        var skins = new SkinCatalog(library.TryRead, data.Nodes, data.EntityNames);
        var set = AnimationTables.Pyrosaur;
        var art = AnimationSheetResolver.Resolve(set, "00", library.TryRead, out _);

        var info = AnimationInfoBuilder.Build(set, "00", set.Anims[0], art, skins, library);

        Assert.Equal(NodeNotes.All[NoteKey.Skin(set, "00")], info.Note);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void A_noted_file_carries_its_note_even_when_the_install_does_not_ship_it()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var info = ResourceInfoBuilder.Build("GM1_PCS.SPF", library);

        Assert.NotNull(info.Note);
    }

    /// <summary>Guards that all file names referenced in NodeNotes prose exist in the reference copy of the game.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Every_file_a_note_names_is_in_the_game()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        foreach (var note in NodeNotes.All.Values)
            foreach (Match m in Regex.Matches(note, @"[A-Z0-9_&]+\.[A-Z]{3}\b"))
                Assert.True(library.Contains(m.Value), $"{m.Value} is not in this copy of the game");
    }
}
