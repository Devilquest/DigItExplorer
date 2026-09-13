using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Menu;

/// <summary>Derives screen placement offsets and color keys for main menu sign pages.</summary>
internal static class MainMenuSignAnchors
{
    /// <summary>Resolved horizontal anchor offset and color key index for a sign page.</summary>
    public readonly record struct Placement(int Anchor, byte ColorKey);

    /// <summary>Minimum score ratio required over the runner-up offset to confirm placement.</summary>
    private const double MinMargin = 1.5;

    /// <summary>Derives the best-matching horizontal anchor and modal color key for a sign page.</summary>
    /// <param name="pageFrame0">Sign sheet frame 0 pixel indices.</param>
    /// <param name="terrainIndices">Level terrain palette indices.</param>
    /// <param name="terrainWidth">Width of the terrain canvas in pixels.</param>
    /// <returns>Derived Placement if match is unambiguous; otherwise, null.</returns>
    public static Placement? Derive(byte[] pageFrame0, byte[] terrainIndices, int terrainWidth)
    {
        if (pageFrame0.Length < FrameCodec.Width * FrameCodec.Height) return null;
        if (terrainWidth < FrameCodec.Width || terrainIndices.Length < terrainWidth * FrameCodec.Height) return null;

        byte colorKey = ModalIndex(pageFrame0);

        int stops = terrainWidth / FrameCodec.Width;
        int bestOffset = -1, bestScore = -1, runnerUpScore = -1;
        for (int s = 0; s < stops; s++)
        {
            int offset = s * FrameCodec.Width;
            int score = ScoreAt(pageFrame0, colorKey, terrainIndices, terrainWidth, offset);
            if (score > bestScore)
            {
                runnerUpScore = bestScore;
                bestOffset = offset;
                bestScore = score;
            }
            else if (score > runnerUpScore)
            {
                runnerUpScore = score;
            }
        }

        if (bestOffset < 0) return null;
        if (runnerUpScore > 0 && bestScore < runnerUpScore * MinMargin) return null;
        return new Placement(bestOffset, colorKey);
    }

    /// <summary>Finds the most frequent palette index in a page frame to use as its color key.</summary>
    private static byte ModalIndex(byte[] page)
    {
        var counts = new int[256];
        foreach (byte v in page) counts[v]++;
        byte modal = 0;
        int max = -1;
        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] <= max) continue;
            max = counts[i];
            modal = (byte)i;
        }
        return modal;
    }

    /// <summary>Calculates the number of exact non-key pixel matches at a specific offset.</summary>
    private static int ScoreAt(byte[] page, byte colorKey, byte[] terrainIndices, int terrainWidth, int offset)
    {
        int score = 0;
        for (int y = 0; y < FrameCodec.Height; y++)
        {
            int pRow = y * FrameCodec.Width;
            int tRow = y * terrainWidth + offset;
            for (int x = 0; x < FrameCodec.Width; x++)
            {
                byte pv = page[pRow + x];
                if (pv == colorKey) continue;
                if (terrainIndices[tRow + x] == pv) score++;
            }
        }
        return score;
    }
}
