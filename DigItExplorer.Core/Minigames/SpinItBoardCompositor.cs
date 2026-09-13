using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Core.Minigames;

/// <summary>Layer visibility options for Spin It! minigame board rendering.</summary>
public sealed record SpinItRenderOptions(bool ShowBackground, bool ShowPointer, bool ShowAttempts);

/// <summary>Composes the Spin It! prize wheel board resting state preview canvas.</summary>
public static class SpinItBoardCompositor
{
    /// <summary>Composes the Spin It! board canvas from game resources and options.</summary>
    /// <param name="loadResource">Resolver function for archive resource bytes.</param>
    /// <param name="options">Layer visibility options.</param>
    /// <returns>Composed MinigameBoardFrame, or null if no layers are visible.</returns>
    public static MinigameBoardFrame? Compose(Func<string, byte[]?> loadResource, SpinItRenderOptions options)
    {
        if (!options.ShowBackground && !options.ShowPointer && !options.ShowAttempts) return null;

        var scrnBytes = loadResource("GM2_SCRN.SPF");
        if (scrnBytes is null) return null;
        var scrn = SheetImage.Read(scrnBytes);

        var rgb = options.ShowBackground
            ? MinigameBoardCommon.ToRgb(scrn.Frames[0], scrn.Palette)
            : new byte[MinigameBoardPositions.Width * MinigameBoardPositions.Height * 3];

        var coverage = options.ShowBackground
            ? RgbCanvas.FullCoverage(MinigameBoardPositions.Width, MinigameBoardPositions.Height)
            : new byte[MinigameBoardPositions.Width * MinigameBoardPositions.Height];

        if (options.ShowAttempts)
            MinigameBoardCommon.StampAttempts(rgb, coverage, MinigameBoard.SpinIt, scrn.Palette, loadResource);

        if (options.ShowPointer)
        {
            var pointer = AnimationTables.SpinIt;
            var spinBytes = loadResource($"{pointer.FixedSheet}.SPF") ?? loadResource($"{pointer.FixedSheet}.MPF");
            if (spinBytes is not null)
            {
                var spinSheet = SheetImage.Read(spinBytes); // UseEmbeddedPalette: this is the pointer's real palette
                var rect = pointer.Rects![0];
                var cell = SpriteSheetSlicer.RectCell(spinSheet.Frames[rect.Page], rect.X, rect.Y,
                    rect.X + rect.W - 1, rect.Y + rect.H - 1);
                MinigameBoardCommon.Blit(rgb, coverage, MinigameBoardPositions.Width, MinigameBoardPositions.Height,
                    MinigameBoardPositions.SpinItPointerX, MinigameBoardPositions.SpinItPointerY, cell,
                    spinSheet.Palette, pointer.TransparentIndices);
            }
        }

        return new MinigameBoardFrame(rgb, coverage, MinigameBoardPositions.Width, MinigameBoardPositions.Height);
    }
}
