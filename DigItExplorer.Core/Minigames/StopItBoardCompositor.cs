using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Core.Minigames;

/// <summary>Layer visibility options for Stop It! minigame board rendering.</summary>
public sealed record StopItRenderOptions(bool ShowBackground, bool ShowPieces, bool ShowLabels, bool ShowAttempts);

/// <summary>Composes the Stop It! slot-machine board resting state preview canvas.</summary>
public static class StopItBoardCompositor
{
    // GM1_PCS/GM3_PCS key out palette index 1 (the navy square each cell is painted on), not the usual 0.
    private static readonly HashSet<byte> NavyKey = [1];

    /// <summary>Composes the Stop It! board canvas from game resources and options.</summary>
    /// <param name="loadResource">Resolver function for archive resource bytes.</param>
    /// <param name="font">Game font used for drawing reel labels.</param>
    /// <param name="labels">Reel label string metadata.</param>
    /// <param name="options">Layer visibility options.</param>
    /// <returns>Composed MinigameBoardFrame, or null if no layers are visible.</returns>
    public static MinigameBoardFrame? Compose(Func<string, byte[]?> loadResource, GameFont font,
        StopItLabels labels, StopItRenderOptions options)
    {
        if (!options.ShowBackground && !options.ShowPieces && !options.ShowLabels && !options.ShowAttempts)
            return null;

        var scrnBytes = loadResource("GM1_SCRN.SPF");
        if (scrnBytes is null) return null;
        var scrn = SheetImage.Read(scrnBytes);
        var palette = scrn.Palette;

        var rgb = options.ShowBackground
            ? MinigameBoardCommon.ToRgb(scrn.Frames[0], palette)
            : new byte[MinigameBoardPositions.Width * MinigameBoardPositions.Height * 3];

        var coverage = options.ShowBackground
            ? RgbCanvas.FullCoverage(MinigameBoardPositions.Width, MinigameBoardPositions.Height)
            : new byte[MinigameBoardPositions.Width * MinigameBoardPositions.Height];

        if (options.ShowPieces)
        {
            var pcsBytes = loadResource("GM1_PCS.SPF");
            if (pcsBytes is not null)
            {
                var pcs = SheetImage.Read(pcsBytes);
                var (x1, y1, x2, y2) = MinigameBoardPositions.PanelPieceCell;
                var cell = SpriteSheetSlicer.RectCell(pcs.Frames[0], x1, y1, x2, y2);
                foreach (int x in MinigameBoardPositions.PanelX)
                    MinigameBoardCommon.Blit(rgb, coverage, MinigameBoardPositions.Width, MinigameBoardPositions.Height,
                        x, MinigameBoardPositions.PanelY, cell, palette, NavyKey);
            }
        }

        if (options.ShowLabels)
        {
            var mask = new byte[MinigameBoardPositions.Width * MinigameBoardPositions.Height];
            var focusedRamp = GameFont.Ramp(labels.FocusedStyle);
            var unfocusedRamp = GameFont.Ramp(labels.UnfocusedStyle);

            for (int i = 0; i < MinigameBoardPositions.PanelX.Length; i++)
            {
                // A round opens with focus on the first reel, so that one carries both the prompt and the
                // focused ink while the other two are turning unfocused.
                bool focused = i == 0;
                int cx = MinigameBoardPositions.PanelX[i] + MinigameBoardPositions.PanelCellW / 2;
                font.DrawCentered(mask, MinigameBoardPositions.Width, MinigameBoardPositions.Height,
                    cx, MinigameBoardPositions.PanelLabelY,
                    focused ? labels.Focused : labels.Spinning,
                    focused ? focusedRamp : unfocusedRamp);
            }

            GameFontLabel.Composite(rgb, coverage, mask, MinigameBoardPositions.Width,
                MinigameBoardPositions.Height, palette);
        }

        if (options.ShowAttempts)
            MinigameBoardCommon.StampAttempts(rgb, coverage, MinigameBoard.StopIt, palette, loadResource);

        return new MinigameBoardFrame(rgb, coverage, MinigameBoardPositions.Width, MinigameBoardPositions.Height);
    }
}
