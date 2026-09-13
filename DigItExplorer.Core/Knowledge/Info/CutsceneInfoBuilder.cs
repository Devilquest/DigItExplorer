using System.Globalization;
using System.Text;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Cutscenes;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for intro and title cutscene pieces.</summary>
public static class CutsceneInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for a cutscene piece.</summary>
    public static NodeInfo Build(CutscenePiece piece, CutsceneData data, CutsceneClip clip,
        ResourceLibrary library)
    {
        var info = data.Pieces[piece];
        bool isCard = info.FileName is null;

        var identity = InfoSections.Of("Identity",
            InfoRow.OrNull("Text", isCard ? Latin1(data.CardText) : null),
            InfoRow.OrNull("Caption", info.Caption is { } caption ? Latin1(caption) : null));

        var source = InfoSource.Section(library, isCard ? data.CardPaletteFile : info.FileName!);

        var contentRows = new List<InfoRow?>
        {
            new InfoRow("Dimensions", $"{FrameCodec.Width}×{FrameCodec.Height} px"),
            new InfoRow("Frames", clip.Frames.Count.ToString()),
            new InfoRow("Iterations", clip.FrameOrder.Count.ToString()),
            new InfoRow("Frame", $"{clip.FrameMs.ToString("0.#", CultureInfo.InvariantCulture)} ms"),
            new InfoRow("Ticks/frame", info.Ticks.ToString()),
            InfoRow.OrNull("Hold", HoldOf(piece, data)),
        };
        var contents = InfoSections.Of("Contents", [.. contentRows]);

        return NodeInfos.Of(null, identity, source, contents);
    }

    /// <summary>How long a piece that ends on a still holds it, in the units the piece's own loop counts in.</summary>
    private static string? HoldOf(CutscenePiece piece, CutsceneData data) => piece switch
    {
        CutscenePiece.Card => $"{data.CardHoldIterations} iterations",
        CutscenePiece.ManLogo => $"{data.LogoHoldTicks} ticks",
        _ => null,
    };

    private static string Latin1(byte[] bytes) => Encoding.Latin1.GetString(bytes);
}
