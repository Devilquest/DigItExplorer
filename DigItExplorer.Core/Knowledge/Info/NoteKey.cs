using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Unique identity key for associating standing notes with tree nodes, skins, or files.</summary>
internal readonly record struct NoteKey
{
    /// <summary>The key in text form.</summary>
    public string Id { get; private init; }

    private static NoteKey Of(string id) => new() { Id = id };

    /// <summary>A whole character: the note reads on every skin and every animation of it.</summary>
    public static NoteKey Character(CharacterAnimSet set) => Of($"character:{set.Name}");

    /// <summary>One world skin of a character, by the sheet suffix <see cref="CharacterSkin.Suffix"/>
    /// carries.</summary>
    public static NoteKey Skin(CharacterAnimSet set, string suffix) => Of($"skin:{set.Name}/{suffix}");

    /// <summary>One animation of one skin, which is the finest grain the tree offers: an animation leaf is
    /// the only thing under a character that can be selected.</summary>
    public static NoteKey Animation(CharacterAnimSet set, string suffix, AnimationDef anim)
        => Of($"animation:{set.Name}/{suffix}/{anim.Name}");

    /// <summary>One dig-spot state.</summary>
    public static NoteKey DigSpot(DigSpotState state) => Of($"digspot:{state}");

    /// <summary>One drain state.</summary>
    public static NoteKey Drain(DrainState state) => Of($"drain:{state}");

    /// <summary>Creates a note key for a raw filename in uppercase invariant form.</summary>
    public static NoteKey File(string name) => Of($"file:{name.ToUpperInvariant()}");
}
