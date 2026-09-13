using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Playback facts for a cutscene piece including filename, driver tick wait, and caption.</summary>
/// <param name="FileName">The animation file, or null for the text card.</param>
/// <param name="Ticks">Driver ticks waited per frame.</param>
/// <param name="Caption">The caption text, or null when the piece has none.</param>
public sealed record CutsceneInfo(string? FileName, int Ticks, byte[]? Caption);

/// <summary>Intro and title cutscene playback values, captions, and geometry loaded from <c>MAIN.EXE</c>.</summary>
public sealed class CutsceneData
{
    /// <summary>The extension the engine's loader supplies when a piece name carries none, which is why the
    /// strings in the executable are bare stems like <c>INTRO01</c>.</summary>
    private const string AnimationExtension = ".ANI";

    // The loader searches the name for a '.' and only appends when it finds none, so a name that already
    // carries an extension is passed through rather than given a second one.
    private static string WithExtension(string name) => name.Contains('.') ? name : name + AnimationExtension;

    // Fixed invocation site opcode patterns (push cs; push di; ...; push mode; push bp).
    private static readonly byte[] PushCsDi = [0x0E, 0x57];

    private readonly Dictionary<CutscenePiece, CutsceneInfo> _pieces = [];

    private CutsceneData() { }

    /// <summary>Per-piece playback facts, keyed by piece.</summary>
    public IReadOnlyDictionary<CutscenePiece, CutsceneInfo> Pieces => _pieces;

    /// <summary>The text card's line, as the game stores it.</summary>
    public byte[] CardText { get; private set; } = [];

    /// <summary>Center x and y the card's line is drawn at.</summary>
    public int CardX { get; private set; }

    /// <inheritdoc cref="CardX"/>
    public int CardY { get; private set; }

    /// <summary>The palette the card is drawn under.</summary>
    public string CardPaletteFile { get; private set; } = "";

    /// <summary>Iterations the card holds before the sequence moves on.</summary>
    public int CardHoldIterations { get; private set; }

    /// <summary>Iterations the caption draws for and holds the animation's first frame.</summary>
    public int CaptionHoldIterations { get; private set; }

    /// <summary>The x the caption is centered on.</summary>
    public int CaptionCenterX { get; private set; }

    /// <summary>The caption's y before the slide starts.</summary>
    public int CaptionStartY { get; private set; }

    /// <summary>The iteration the caption starts sliding on, and the pixels it gains per iteration after.</summary>
    public int CaptionSlideFrom { get; private set; }

    /// <inheritdoc cref="CaptionSlideFrom"/>
    public int CaptionSlidePx { get; private set; }

    /// <summary>Passes the logo prologue's loop makes, which is fewer than the animation has frames.</summary>
    public int LogoPasses { get; private set; }

    /// <summary>Driver ticks the prologue holds the frame its loop left up for, before the game's own title
    /// sequence starts.</summary>
    public int LogoHoldTicks { get; private set; }

    /// <summary>Parses all cutscene piece invocations, text card data, and caption geometry from the executable.</summary>
    internal static bool TryRead(ExeReader reader, out CutsceneData data)
    {
        data = new CutsceneData();
        return data.ReadIntroPieces(reader)
            && data.ReadTitlePiece(reader)
            && data.ReadLogoPiece(reader)
            && data.ReadCard(reader)
            && data.ReadCaptionGeometry(reader);
    }

    // Numbered 01, 03, 02: the order the call sites run in, not the order the names suggest.
    private bool ReadIntroPieces(ExeReader reader)
    {
        CutscenePiece[] pieces = [CutscenePiece.Intro01, CutscenePiece.Intro03, CutscenePiece.Intro02];
        if (!reader.TryReadTickWait(ExeLayout.IntroTickSite, out int ticks)) return false;

        for (int i = 0; i < pieces.Length; i++)
        {
            var site = ExeLayout.IntroPieceSites[i];
            if (!reader.TryReadPointer(site, out var namePointer)) return false;
            if (!reader.Matches(site + 3, PushCsDi)) return false;
            if (!reader.TryReadPointer(site + 5, out var captionPointer)) return false;
            if (!reader.Matches(site + 8, PushCsDi)) return false;

            if (!reader.TryReadPascalString(namePointer, out var name)) return false;
            if (!reader.TryReadPascalString(captionPointer, out var caption)) return false;

            _pieces[pieces[i]] = new CutsceneInfo(WithExtension(Latin1(name)), ticks,
                caption.Length > 0 ? caption : null);
        }

        // INTRO00 is never invoked, so its entry is composed rather than read from a call site.
        _pieces[CutscenePiece.Intro00] = new CutsceneInfo("INTRO00.MPF", ticks, Caption: null);
        return true;
    }

    // Title animation plays from its own inline loop with custom tick wait.
    private bool ReadTitlePiece(ExeReader reader)
    {
        if (!reader.TryReadPointer(ExeLayout.TitleAnimationNameSite, out var namePointer)) return false;
        if (!reader.TryReadPascalString(namePointer, out var name)) return false;
        if (!reader.TryReadTickWait(ExeLayout.TitleTickSite, out int ticks)) return false;

        _pieces[CutscenePiece.DigTitle] = new CutsceneInfo(WithExtension(Latin1(name)), ticks, Caption: null);
        return true;
    }

    // Only one build carries the prologue, so a build without it leaves the piece out rather than failing:
    // the tree already handles a piece whose file is not there to play.
    private bool ReadLogoPiece(ExeReader reader)
    {
        if (!reader.PlaysLogoPrologue) return true;
        if (!reader.TryReadPointer(ExeLayout.LogoNameSite, out var namePointer)) return false;
        if (!reader.TryReadPascalString(namePointer, out var name)) return false;
        if (!reader.TryReadTickWait(ExeLayout.LogoTickSite, out int ticks)) return false;
        if (!reader.TryReadLocalThreshold(ExeLayout.LogoPassSite, out int passes)) return false;
        if (!reader.TryReadTickWait(ExeLayout.LogoHoldSite, out int holdTicks)) return false;

        LogoPasses = passes;
        LogoHoldTicks = holdTicks;
        _pieces[CutscenePiece.ManLogo] = new CutsceneInfo(WithExtension(Latin1(name)), ticks, Caption: null);
        return true;
    }

    private bool ReadCard(ExeReader reader)
    {
        if (!reader.TryReadTextCallSite(ExeLayout.CardTextSite, out var site)) return false;
        if (!reader.TryReadPascalString(site.String, out var text)) return false;
        if (!reader.TryReadPointer(ExeLayout.CardPaletteSite, out var palettePointer)) return false;
        if (!reader.TryReadPascalString(palettePointer, out var palette)) return false;
        if (!reader.TryReadLocalThreshold(ExeLayout.CardHoldSite, out int hold)) return false;
        if (!reader.TryReadTickWait(ExeLayout.CardTickSite, out int ticks)) return false;

        CardText = text;
        CardX = site.X;
        CardY = site.Y;
        CardPaletteFile = Latin1(palette);
        CardHoldIterations = hold;
        _pieces[CutscenePiece.Card] = new CutsceneInfo(FileName: null, ticks, Caption: null);
        return true;
    }

    private bool ReadCaptionGeometry(ExeReader reader)
    {
        if (!reader.TryReadCounterThreshold(ExeLayout.CaptionHoldSite, out int hold)) return false;
        if (!reader.TryReadCounterThreshold(ExeLayout.CaptionSlideFromSite, out int slideFrom)) return false;
        if (!reader.TryReadCaptionSlideStep(ExeLayout.CaptionSlidePxSite, out int slidePx)) return false;
        if (!reader.TryReadCaptionStartY(ExeLayout.CaptionYSite, out int startY)) return false;
        if (!reader.TryReadPushImm16(ExeLayout.CaptionCenterXSite, out int centerX)) return false;

        CaptionHoldIterations = hold;
        CaptionSlideFrom = slideFrom;
        CaptionSlidePx = slidePx;
        CaptionStartY = startY;
        CaptionCenterX = centerX;
        return true;
    }

    private static string Latin1(byte[] bytes) => System.Text.Encoding.Latin1.GetString(bytes);
}
