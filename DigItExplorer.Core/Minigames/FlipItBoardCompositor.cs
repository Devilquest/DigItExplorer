using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Core.Minigames;

/// <summary>Layer visibility options for Flip It! minigame board rendering.</summary>
public sealed record FlipItRenderOptions(bool ShowBackground, bool ShowRopes, bool ShowCards, bool ShowAttempts);

/// <summary>Composes the Flip It! memory-card board resting state preview canvas.</summary>
public static class FlipItBoardCompositor
{
    /// <summary>Composes the Flip It! board canvas from game resources and options.</summary>
    /// <param name="loadResource">Resolver function for archive resource bytes.</param>
    /// <param name="options">Layer visibility options.</param>
    /// <returns>Composed MinigameBoardFrame, or null if no layers are visible.</returns>
    public static MinigameBoardFrame? Compose(Func<string, byte[]?> loadResource, FlipItRenderOptions options)
    {
        if (!options.ShowBackground && !options.ShowRopes && !options.ShowCards && !options.ShowAttempts) return null;

        var paraBytes = loadResource("GM0_PARA.SPF");
        if (paraBytes is null) return null;
        var para = SheetImage.Read(paraBytes);

        var palBytes = loadResource("GM0_PAL.PAL");
        var palette = palBytes is { Length: >= 768 } ? VgaPalette.From6Bit(palBytes.AsSpan(0, 768)) : para.Palette;

        var rgb = options.ShowBackground
            ? MinigameBoardCommon.ToRgb(para.Frames[0], palette)
            : new byte[MinigameBoardPositions.Width * MinigameBoardPositions.Height * 3];

        var coverage = options.ShowBackground
            ? RgbCanvas.FullCoverage(MinigameBoardPositions.Width, MinigameBoardPositions.Height)
            : new byte[MinigameBoardPositions.Width * MinigameBoardPositions.Height];

        if (options.ShowRopes)
        {
            var midfBytes = loadResource("GM0_MIDF.SPF");
            if (midfBytes is not null)
            {
                var midf = SheetImage.Read(midfBytes);
                var cell = new SpriteCell(0, 0, MinigameBoardPositions.Width, MinigameBoardPositions.Height, midf.Frames[0]);
                MinigameBoardCommon.Blit(rgb, coverage, MinigameBoardPositions.Width, MinigameBoardPositions.Height, 0, 0, cell, palette);
            }
        }

        if (options.ShowCards)
        {
            var turnBytes = loadResource("GM0_TURN.SPF") ?? loadResource("GM0_TURN.MPF");
            if (turnBytes is not null)
            {
                var turn = SheetImage.Read(turnBytes);
                var rects = SkinCatalog.RectsOf(AnimationTables.FlipIt, turn.Frames);
                if (rects is not null && rects.TryGetValue(0, out var rect))
                {
                    var cell = SpriteSheetSlicer.RectCell(turn.Frames[rect.Page], rect.X, rect.Y,
                        rect.X + rect.W - 1, rect.Y + rect.H - 1);
                    foreach (int x in MinigameBoardPositions.FlipItCardX)
                        foreach (int y in MinigameBoardPositions.FlipItCardY)
                            MinigameBoardCommon.Blit(rgb, coverage, MinigameBoardPositions.Width, MinigameBoardPositions.Height,
                                x, y, cell, palette);
                }
            }
        }

        if (options.ShowAttempts)
            MinigameBoardCommon.StampAttempts(rgb, coverage, MinigameBoard.FlipIt, palette, loadResource);

        return new MinigameBoardFrame(rgb, coverage, MinigameBoardPositions.Width, MinigameBoardPositions.Height);
    }
}
