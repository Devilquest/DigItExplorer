using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Story;

namespace DigItExplorer.Core.Cutscenes;

/// <summary>Reconstructs and composes cutscene animations and text cards from game resources.</summary>
public static class CutsceneCompositor
{
    /// <summary>Driver tick duration in milliseconds (measured at 42.857 ms / 6).</summary>
    public const double TickMs = StoryReconstructor.FrameMs / 6;

    /// <summary>Builds an animation clip for a cutscene piece, optionally rendering caption overlays.</summary>
    public static CutsceneClip Build(CutscenePiece piece, SheetImage ani, GameFont font, CutsceneData data)
    {
        var info = data.Pieces[piece];
        bool hasCaption = info.Caption is not null;
        var overlay = hasCaption ? RenderCaption(font, info.Caption!, data) : null;
        var order = piece == CutscenePiece.ManLogo
            ? LogoFrameOrder(ani.Frames.Count, info.Ticks, data)
            : FrameOrder(ani.Frames.Count, hasCaption, data.CaptionHoldIterations);
        return new CutsceneClip(ani.Frames, ani.Palette, order, info.Ticks * TickMs, overlay);
    }

    /// <summary>Builds a text-only title card cutscene clip held for a fixed duration.</summary>
    public static CutsceneClip BuildCard(GameFont font, VgaPalette palette, CutsceneData data)
    {
        var info = data.Pieces[CutscenePiece.Card];
        var frame = new byte[FrameCodec.FrameBytes];
        font.DrawCentered(frame, FrameCodec.Width, FrameCodec.Height, data.CardX, data.CardY, data.CardText);
        var order = new int[data.CardHoldIterations]; // all zero: the clip's only frame
        return new CutsceneClip([frame], palette, order, info.Ticks * TickMs, Caption: null);
    }

    /// <summary>Composes a specific frame iteration for a cutscene clip with sliding caption blits.</summary>
    public static byte[] Compose(CutsceneClip clip, int iteration, CutsceneData data)
    {
        var frame = (byte[])clip.Frames[clip.FrameOrder[iteration]].Clone();
        if (clip.Caption is { } caption && CaptionYAt(iteration, data) is { } y)
            Blit(frame, caption, y);
        return frame;
    }

    /// <summary>Calculates the vertical position of the caption overlay for a given playback iteration.</summary>
    private static int? CaptionYAt(int iteration, CutsceneData data) =>
        iteration >= data.CaptionHoldIterations
            ? null
            : data.CaptionStartY + data.CaptionSlidePx * Math.Max(0, iteration - data.CaptionSlideFrom);

    /// <summary>Generates the sequence of frame indices accounting for initial caption hold iterations.</summary>
    private static int[] FrameOrder(int frameCount, bool hasCaption, int holdIterations)
    {
        if (!hasCaption) return [.. Enumerable.Range(0, frameCount)];
        var order = new int[holdIterations + frameCount];
        for (int i = 0; i < frameCount; i++) order[holdIterations + i] = i;
        return order;
    }

    /// <summary>Generates the logo prologue's own sequence: the frames its loop gets through, then the last
    /// of those repeated for the hold that follows it.</summary>
    private static int[] LogoFrameOrder(int frameCount, int ticks, CutsceneData data)
    {
        int shown = Math.Min(data.LogoPasses, frameCount);
        if (shown <= 0) return [];

        // The hold neither presents nor advances, so its own pacing changes nothing on screen and only its
        // total matters, counted here in the frames of this clip rather than in the passes the game makes.
        int held = ticks > 0 ? data.LogoHoldTicks / ticks : 0;
        var order = new int[shown + held];
        for (int i = 0; i < shown; i++) order[i] = i;
        for (int i = shown; i < order.Length; i++) order[i] = shown - 1;
        return order;
    }

    private static CaptionOverlay RenderCaption(GameFont font, byte[] text, CutsceneData data)
    {
        int width = font.TextWidth(text);
        int x = data.CaptionCenterX - (width >> 1); // the engine halves with a right shift, not a true /2
        var pixels = new byte[width * GameFont.CellSize];
        font.Draw(pixels, width, GameFont.CellSize, 0, 0, text);
        return new CaptionOverlay(pixels, width, GameFont.CellSize, x);
    }

    private static void Blit(byte[] frame, CaptionOverlay caption, int y)
    {
        for (int row = 0; row < caption.Height; row++)
        {
            int fy = y + row;
            if (fy < 0 || fy >= FrameCodec.Height) continue;
            int srcBase = row * caption.Width;
            int dstBase = fy * FrameCodec.Width;
            for (int col = 0; col < caption.Width; col++)
            {
                int fx = caption.X + col;
                if (fx < 0 || fx >= FrameCodec.Width) continue;
                byte v = caption.Pixels[srcBase + col];
                if (v != 0) frame[dstBase + fx] = v;
            }
        }
    }
}
