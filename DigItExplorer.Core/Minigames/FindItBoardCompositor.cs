using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Minigames;

/// <summary>Layer visibility options for Find It! minigame board rendering.</summary>
public sealed record FindItRenderOptions(bool ShowBackground, bool ShowPieces, bool ShowLabels, bool ShowAttempts);

/// <summary>Composes the Find It! shell-game board resting state preview canvas.</summary>
public static class FindItBoardCompositor
{
    // GM1_PCS/GM3_PCS key out palette index 1 (the navy square each cell is painted on), not the usual 0.
    private static readonly HashSet<byte> NavyKey = [1];

    /// <summary>Composes the Find It! board canvas from game resources and options.</summary>
    /// <param name="loadResource">Resolver function for archive resource bytes.</param>
    /// <param name="font">Game font used for drawing panel labels.</param>
    /// <param name="startLabel">Decoded bytes of the start label string.</param>
    /// <param name="options">Layer visibility options.</param>
    /// <returns>Composed MinigameBoardFrame, or null if no layers are visible.</returns>
    public static MinigameBoardFrame? Compose(Func<string, byte[]?> loadResource, GameFont font,
        byte[] startLabel, FindItRenderOptions options)
    {
        if (!options.ShowBackground && !options.ShowPieces && !options.ShowLabels && !options.ShowAttempts)
            return null;

        var scrnBytes = loadResource("GM3_SCRN.SPF");
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
            var pcsBytes = loadResource("GM3_PCS.SPF");
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
            foreach (int x in MinigameBoardPositions.PanelX)
            {
                int cx = x + MinigameBoardPositions.PanelCellW / 2;
                font.DrawCentered(mask, MinigameBoardPositions.Width, MinigameBoardPositions.Height,
                    cx, MinigameBoardPositions.PanelLabelY, startLabel);
            }

            GameFontLabel.Composite(rgb, coverage, mask, MinigameBoardPositions.Width,
                MinigameBoardPositions.Height, palette);
        }

        if (options.ShowAttempts)
            MinigameBoardCommon.StampAttempts(rgb, coverage, MinigameBoard.FindIt, palette, loadResource);

        return new MinigameBoardFrame(rgb, coverage, MinigameBoardPositions.Width, MinigameBoardPositions.Height);
    }
}
