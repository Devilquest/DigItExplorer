using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Ending sequence captions, animation filename, and music track parsed from <c>MAIN.EXE</c>.</summary>
public sealed class EndSequenceData
{
    private EndSequenceData(string fileName, string tuneName,
        IReadOnlyList<IReadOnlyList<EndSequenceCaption>> frames)
    {
        FileName = fileName;
        TuneName = tuneName;
        Frames = frames;
    }

    /// <summary>The animation file the sequence plays its screens from.</summary>
    public string FileName { get; }

    /// <summary>The tune it plays under.</summary>
    public string TuneName { get; }

    /// <summary>Captions for each of the 10 ending screens in display order.</summary>
    public IReadOnlyList<IReadOnlyList<EndSequenceCaption>> Frames { get; }

    /// <summary>Parses ending sequence captions and media filenames from the executable.</summary>
    internal static bool TryRead(ExeReader reader, out EndSequenceData data)
    {
        data = null!;

        if (!reader.TryReadPascalString(ExeLayout.EndSequenceTuneName, out var tune)) return false;
        if (!reader.TryReadPascalString(ExeLayout.EndSequenceTuneName + 1 + tune.Length, out var file)) return false;

        var frames = new List<IReadOnlyList<EndSequenceCaption>>(EndSequenceCatalog.Sites.Count);

        foreach (var screen in EndSequenceCatalog.Sites)
        {
            var captions = new List<EndSequenceCaption>(screen.Count);
            foreach (var site in screen)
            {
                if (!reader.TryReadTextCallSite(site.Site, out var call)) return false;
                if (!reader.TryReadPascalString(call.String, out var text)) return false;
                if (!reader.TryReadPushedWord(site.StyleSite, out int style, out _)) return false;

                captions.Add(new EndSequenceCaption(text, call.X, call.Y, site.Align, style));
            }
            frames.Add(captions);
        }

        data = new EndSequenceData(System.Text.Encoding.Latin1.GetString(file),
            System.Text.Encoding.Latin1.GetString(tune), frames);
        return true;
    }
}
