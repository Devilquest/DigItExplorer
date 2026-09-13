using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Instructions and Credits slab screen dimensions, stop counts, and counter templates from <c>MAIN.EXE</c>.</summary>
public sealed class SlabData
{
    /// <summary>Camera movement step per navigation keypress in pixels.</summary>
    public const int SlabStep = 150;

    /// <summary>Screen index for Instructions, into <see cref="SlabCount"/> and the parallax/config table.</summary>
    public const int Instructions = 0;

    /// <summary>Screen index for Credits.</summary>
    public const int Credits = 1;

    private readonly int[] _slabCounts; // [Instructions, Credits]: max index + 1

    private SlabData(int[] slabCounts, int width, int height, string parallaxFile,
        byte[] labelPrefix, byte[] labelInfix)
    {
        _slabCounts = slabCounts;
        Width = width;
        Height = height;
        ParallaxFile = parallaxFile;
        LabelPrefix = labelPrefix;
        LabelInfix = labelInfix;
    }

    /// <summary>Resting stop count for specified screen index (Instructions or Credits).</summary>
    public int SlabCount(int screen) => _slabCounts[screen];

    /// <summary>Stitched canvas width in pixels.</summary>
    public int Width { get; }

    /// <summary>Stitched canvas height in pixels.</summary>
    public int Height { get; }

    /// <summary>Parallax backdrop resource filename played behind slab screens.</summary>
    public string ParallaxFile { get; }

    /// <summary>Counter template text preceding the digit sequence (e.g. 'Slab ').</summary>
    public byte[] LabelPrefix { get; }

    /// <summary>Counter template text following the digit sequence (e.g. ' of ').</summary>
    public byte[] LabelInfix { get; }

    /// <summary>Parses slab configuration words and counter template from the executable.</summary>
    internal static bool TryRead(ExeReader reader, out SlabData data)
    {
        data = null!;

        var words = new int[ExeLayout.SlabConfigWordCount];
        for (int i = 0; i < words.Length; i++)
        {
            if (!reader.TryReadWord(ExeLayout.SlabConfigTable + i * 2, out words[i])) return false;
        }
        int maxIndexInstructions = words[0], maxIndexCredits = words[1];
        int width = words[2], height = words[3], parallaxSet = words[4];

        if (width != FrameCodec.Width) return false;
        if (height <= 0 || height % FrameCodec.Height != 0) return false;

        var slabCounts = new[] { maxIndexInstructions + 1, maxIndexCredits + 1 };
        foreach (int count in slabCounts)
        {
            if (count < 1) return false;
            if ((count - 1) * SlabStep + FrameCodec.Height > height) return false;
        }

        if (!reader.TryReadPascalString(ExeLayout.SlabCounterTemplate, out var template) || template.Length == 0)
            return false;
        if (!TrySplitOnDigitRun(template, out var prefix, out var infix)) return false;

        string parallaxFile = $"PARA{parallaxSet:D2}B.MPF";
        data = new SlabData(slabCounts, width, height, parallaxFile, prefix, infix);
        return true;
    }

    // The template is the game's own "Slab 1 of ", whose digits the counter replaces.
    private static bool TrySplitOnDigitRun(byte[] template, out byte[] prefix, out byte[] infix)
    {
        prefix = []; infix = [];
        int digitStart = Array.FindIndex(template, b => b is >= (byte)'0' and <= (byte)'9');
        if (digitStart < 0) return false;

        int digitEnd = digitStart;
        while (digitEnd < template.Length && template[digitEnd] is >= (byte)'0' and <= (byte)'9') digitEnd++;
        if (digitEnd == template.Length) return false; // nothing after the digit run to split off

        prefix = template[..digitStart];
        infix = template[digitEnd..];
        return true;
    }
}
