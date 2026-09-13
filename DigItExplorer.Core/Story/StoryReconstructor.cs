using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Core.Story;

/// <summary>Reconstructs Traces of Dugette story scenes with word-wrapped scrolling text.</summary>
public static class StoryReconstructor
{
    private const int WrapPx = 200;
    private const int LinePitch = 14;
    private const int FirstY = 198;
    private const int CanvasHeight = 1600;
    private const int FirstPause = 46;
    private const int ScrollFrames = 14;
    private const int PauseFrames = 23;

    private const int TextTop = 2;
    private const int TextBottom = 197;

    /// <summary>Frame tick length in milliseconds (3 VGA vsync intervals, approx 42.857 ms).</summary>
    public const double FrameMs = 3000.0 / 70;

    private static readonly IReadOnlyDictionary<int, int> WorldMode = new Dictionary<int, int> { [0] = 1, [1] = 1, [2] = 0, [3] = 0 };

    /// <summary>Builds a story scene for a given game world using world-specific text column alignment.</summary>
    public static StoryScene Build(GameFont font, SheetImage background, byte[] storyTxt, World? world) =>
        Build(font, background, storyTxt,
            world.HasValue && WorldMode.TryGetValue((int)world.Value, out var mode) ? mode : 0);

    /// <summary>Builds a story scene from an explicit text column alignment mode.</summary>
    public static StoryScene Build(GameFont font, SheetImage background, byte[] storyTxt, int mode)
    {
        var lines = ReadLines(storyTxt);
        var (displayLines, total) = LayOut(lines, font);
        int textX = mode == 0 ? 8 : 112;

        var canvas = new byte[FrameCodec.Width * CanvasHeight];
        foreach (var (y, text) in displayLines)
            font.Draw(canvas, FrameCodec.Width, CanvasHeight, textX, y, text);

        return new StoryScene(background.Frames[0], background.Palette, canvas, CanvasHeight,
            ScrollSchedule(total), total);
    }

    /// <summary>Composes a single tick frame by blitting the scrolled text canvas window over background.</summary>
    public static byte[] Compose(StoryScene scene, int tickIndex)
    {
        var frame = (byte[])scene.Background.Clone();
        int offset = scene.ScrollOffsets[tickIndex];
        for (int row = TextTop; row <= TextBottom; row++)
        {
            int canvasBase = (offset + row) * FrameCodec.Width;
            int frameBase = row * FrameCodec.Width;
            for (int col = 0; col < FrameCodec.Width; col++)
            {
                byte v = scene.TextCanvas[canvasBase + col];
                if (v != 0) frame[frameBase + col] = v;
            }
        }
        return frame;
    }

    /// <summary>Parses story text bytes into clean lines, removing CRLF and trailing blanks.</summary>
    private static List<byte[]> ReadLines(byte[] raw)
    {
        var normalized = new List<byte>(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            if (raw[i] == 0x0D && i + 1 < raw.Length && raw[i + 1] == 0x0A) continue;
            normalized.Add(raw[i]);
        }

        var lines = new List<byte[]>();
        int start = 0;
        for (int i = 0; i < normalized.Count; i++)
        {
            if (normalized[i] == 0x0A)
            {
                lines.Add(normalized.GetRange(start, i - start).ToArray());
                start = i + 1;
            }
        }
        lines.Add(normalized.GetRange(start, normalized.Count - start).ToArray());

        while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return lines;
    }

    /// <summary>Trims leading/trailing spaces and collapses consecutive internal whitespace.</summary>
    private static byte[] Trim(ReadOnlySpan<byte> s)
    {
        int start = 0, end = s.Length;
        while (start < end && s[start] == (byte)' ') start++;
        while (end > start && s[end - 1] == (byte)' ') end--;

        var result = new List<byte>(end - start);
        bool lastWasSpace = false;
        for (int i = start; i < end; i++)
        {
            bool isSpace = s[i] == (byte)' ';
            if (isSpace && lastWasSpace) continue;
            result.Add(s[i]);
            lastWasSpace = isSpace;
        }
        return result.ToArray();
    }

    /// <summary>Word-wraps story lines to fit canvas width and calculates total scroll distance.</summary>
    private static (List<(int Y, byte[] Text)> Lines, int Total) LayOut(List<byte[]> lines, GameFont font)
    {
        var outLines = new List<(int Y, byte[] Text)>();
        int y = FirstY;
        int idx = 0;
        int last = lines.Count - 1;
        byte[] pending = Trim(lines[0]);
        int gap = 0;

        byte[] Source(int i) => i <= last ? lines[i] : [];

        void Emit()
        {
            var chunk = pending;
            while (chunk.Length > 0 && font.TextWidth(chunk) > WrapPx)
                chunk = chunk[..^1];
            while (chunk.Length > 0 && chunk[^1] != (byte)' ')
                chunk = chunk[..^1];

            outLines.Add((y, chunk));
            pending = chunk.Length > 0 ? pending[chunk.Length..] : [];
            gap = Trim(chunk).Length > 0 ? LinePitch : 0;
            y += LinePitch;
        }

        while (true)
        {
            if (font.TextWidth(pending) <= WrapPx)
            {
                idx++;
                var src = Source(idx);
                var joined = new byte[pending.Length + 1 + src.Length];
                pending.CopyTo(joined, 0);
                joined[pending.Length] = (byte)' ';
                src.CopyTo(joined, pending.Length + 1);
                var trimmed = Trim(joined);
                pending = new byte[trimmed.Length + 1];
                trimmed.CopyTo(pending, 0);
                pending[^1] = (byte)' ';
            }
            Emit();
            if (Source(idx).Length == 0)
            {
                while (pending.Length > 0) Emit();
                y += gap;
            }
            if (idx > last) break;
        }
        while (pending.Length > 0) Emit();

        return (outLines, y);
    }

    /// <summary>Generates the per-tick scroll offset timeline including initial and periodic pauses.</summary>
    private static int[] ScrollSchedule(int total)
    {
        var offsets = new List<int>();
        int pause = FirstPause, step = 0, scroll = 0;
        while (true)
        {
            if (pause > 0) { pause--; step = ScrollFrames; }
            else
            {
                scroll++;
                step--;
                if (step == 0) pause = PauseFrames;
            }
            offsets.Add(scroll);
            if (scroll > total) break;
        }
        return offsets.ToArray();
    }
}
