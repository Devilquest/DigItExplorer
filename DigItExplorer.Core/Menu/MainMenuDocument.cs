using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Core.Menu;

/// <summary>Decoded main menu document with level map, background, and sign sheets.</summary>
public sealed class MainMenuDocument
{
    /// <summary>Underlying level document containing terrain, collision, and enemy records.</summary>
    public MapDocument Map { get; }

    /// <summary>Stitched row-major palette indices of the back parallax backdrop.</summary>
    public byte[] BackgroundIndices { get; }

    /// <summary>Back parallax filename referenced in the level header.</summary>
    public string BackgroundFile { get; }

    /// <summary>VGA palette used to color the background indices.</summary>
    public VgaPalette BackgroundPalette { get; }

    /// <summary>Loaded sign sheet images keyed by filename.</summary>
    public IReadOnlyDictionary<string, SheetImage> Signs { get; }

    /// <summary>Derived placement offsets and color keys per sign filename.</summary>
    internal IReadOnlyDictionary<string, MainMenuSignAnchors.Placement> Placements { get; }

    /// <summary>Ordered list of sign filenames supported on the main menu.</summary>
    internal static readonly string[] SignFiles = ["MENU01.MPF", "MENU02.MPF", "MENU03.MPF", "MENU04.MPF"];

    private MainMenuDocument(MapDocument map, string backgroundFile, byte[] backgroundIndices,
        VgaPalette backgroundPalette, IReadOnlyDictionary<string, SheetImage> signs,
        IReadOnlyDictionary<string, MainMenuSignAnchors.Placement> placements)
    {
        Map = map;
        BackgroundFile = backgroundFile;
        BackgroundIndices = backgroundIndices;
        BackgroundPalette = backgroundPalette;
        Signs = signs;
        Placements = placements;
    }

    /// <summary>Loads the main menu document from archive resources.</summary>
    /// <param name="loadResource">Resolver function for archive resource bytes.</param>
    /// <returns>The reconstructed MainMenuDocument, or null if essential assets are missing.</returns>
    public static MainMenuDocument? Load(Func<string, byte[]?> loadResource)
    {
        var map = MapDocument.Load("LVL900", loadResource);
        if (map is null) return null;

        var dlfBytes = loadResource("LVL900.DLF");
        if (dlfBytes is not { Length: >= DlfHeader.SizeWithParaSet }) return null;
        int paraSet = DlfHeader.Read(dlfBytes).ParaSet;

        var backFile = $"PARA{paraSet:D2}B.MPF";
        var backBytes = loadResource(backFile);
        if (backBytes is null) return null;
        var backSheet = SheetImage.Read(backBytes);
        var backgroundIndices = StitchHorizontal(backSheet.Frames, map.Terrain.Width, map.Terrain.Height);
        var backgroundPalette = map.RealPalette ?? backSheet.Palette;

        var signs = new Dictionary<string, SheetImage>();
        var placements = new Dictionary<string, MainMenuSignAnchors.Placement>();
        foreach (var file in SignFiles)
        {
            var bytes = loadResource(file);
            if (bytes is null) continue;
            var sheet = SheetImage.Read(bytes);
            signs[file] = sheet;
            if (sheet.Frames.Count == 0) continue;
            var placement = MainMenuSignAnchors.Derive(sheet.Frames[0], map.Terrain.Indices, map.Terrain.Width);
            if (placement is { } p) placements[file] = p;
        }

        return new MainMenuDocument(map, backFile, backgroundIndices, backgroundPalette, signs, placements);
    }

    /// <summary>Stitches chained 320x200 backdrop frames horizontally into a single canvas.</summary>
    private static byte[] StitchHorizontal(IReadOnlyList<byte[]> frames, int canvasWidth, int canvasHeight)
    {
        var canvas = new byte[canvasWidth * canvasHeight];
        int stops = canvasWidth / FrameCodec.Width;
        for (int s = 0; s < Math.Min(stops, frames.Count); s++)
        {
            var frame = frames[s];
            int xOffset = s * FrameCodec.Width;
            for (int y = 0; y < canvasHeight && y < FrameCodec.Height; y++)
                Array.Copy(frame, y * FrameCodec.Width, canvas, y * canvasWidth + xOffset, FrameCodec.Width);
        }
        return canvas;
    }
}
