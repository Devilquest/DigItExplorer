using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for animation nodes.</summary>
public static class AnimationInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for a character animation.</summary>
    public static NodeInfo Build(CharacterAnimSet set, string suffix, AnimationDef anim,
        ResolvedAnimationSheet? art, SkinCatalog skins, ResourceLibrary library)
    {
        var identity = InfoSections.Of("Identity",
            new InfoRow("Character", skins.CharacterLabel(set)),
            // A fixed single-sheet set has no skin of its own, so the row is dropped.
            InfoRow.OrNull("Skin", skins.TryGetCached(set.Name)?.FirstOrDefault(s => s.Suffix == suffix)?.Label),
            new InfoRow("Animation", skins.AnimLabel(set, anim)));

        var source = art is null
            ? null
            : InfoSource.Of(library, new[] { art.SheetFile, art.PaletteFile }.OfType<string>());

        var contentRows = new List<InfoRow?>
        {
            new InfoRow("Mode", anim.Mode.ToString()),
            new InfoRow("Steps", anim.Frames.Count.ToString()),
            new InfoRow("Frames", anim.Frames.Distinct().Count().ToString()),
        };
        if (art is not null)
        {
            var (cellW, cellH) = SkinCatalog.CellSize(art.Rects, art.Grid, anim);
            contentRows.Add(new InfoRow("Cell", $"{cellW}×{cellH} px"));
            contentRows.Add(new InfoRow("Sheet pages", art.Pages.Count.ToString()));
        }
        if (anim.TicksPerStep != 1)
            contentRows.Add(new InfoRow("Ticks/step", anim.TicksPerStep.ToString()));
        var contents = InfoSections.Of("Contents", [.. contentRows]);

        var note = NodeNotes.For(NoteKey.Animation(set, suffix, anim), NoteKey.Skin(set, suffix),
            NoteKey.Character(set));
        return NodeInfos.Of(note, identity, source, contents);
    }
}
