namespace DigItExplorer.Core.Catalog;

/// <summary>The <c>seg:offset</c> address of every value this application reads out of <c>MAIN.EXE</c>.</summary>
internal static class ExeLayout
{
    /// <summary>The executable every address below is an address in.</summary>
    public const string MainExe = "MAIN.EXE";

    public static ExeAddress Seg1(int offset) => ExeAddress.Recorded(ExeSegment.Seg1, offset);
    public static ExeAddress Seg2(int offset) => ExeAddress.Recorded(ExeSegment.Seg2, offset);
    public static ExeAddress Seg3(int offset) => ExeAddress.Recorded(ExeSegment.Seg3, offset);
    public static ExeAddress DGroup(int offset) => ExeAddress.Recorded(ExeSegment.DGroup, offset);

    // The logo prologue is code only one build carries, so its offsets are that build's own and the shift
    // the rest of seg1 takes has nothing to measure them from.
    private static ExeAddress AddedSeg1(int offset) => ExeAddress.InAddedCode(ExeSegment.Seg1, offset);

    // ---- Intro sequence (seg1:0x46B) ---------------------------------------

    // Bytecode call site layout for intro animation sequences (INTRO01, INTRO03, INTRO02).
    public static readonly ExeAddress[] IntroPieceSites = [Seg1(0x4A3), Seg1(0x4C2), Seg1(0x585)];

    /// <summary>The title's own <c>mov di,&lt;name&gt;</c> for the animation its inline loop plays (seg1:0x2825).</summary>
    public static readonly ExeAddress TitleAnimationNameSite = Seg1(0x2825);

    // ---- Logo prologue, in the build that plays one (seg1:0x2816) ----------

    /// <summary>The prologue's <c>mov di,&lt;name&gt;</c> for the animation it plays (seg1:0x2820).</summary>
    public static readonly ExeAddress LogoNameSite = AddedSeg1(0x2820);

    /// <summary>Its loop's <c>cmp ax, 9</c>: the ticks it holds each frame for (seg1:0x2861).</summary>
    public static readonly ExeAddress LogoTickSite = AddedSeg1(0x2861);

    /// <summary>Its <c>cmp word [bp-2], 0x3A</c>: the passes the loop makes, which is fewer than the
    /// animation has frames (seg1:0x2882).</summary>
    public static readonly ExeAddress LogoPassSite = AddedSeg1(0x2882);

    /// <summary>The following hold's <c>cmp ax, 0x12C</c>: the ticks the frame the loop left up stays up
    /// for (seg1:0x28B2).</summary>
    public static readonly ExeAddress LogoHoldSite = AddedSeg1(0x28B2);

    /// <summary>The text card's draw call (seg1:0x4FE) (a `push x; push y; mov di,&lt;string&gt;` triplet, the
    /// same shape every ending caption uses).</summary>
    public static readonly ExeAddress CardTextSite = Seg1(0x4FE);

    /// <summary>The <c>mov di,&lt;name&gt;</c> of the palette load that precedes the card (seg1:0x4D2).</summary>
    public static readonly ExeAddress CardPaletteSite = Seg1(0x4D2);

    // ---- Playback constants (seg1) -----------------------------------------
    public static readonly ExeAddress CaptionHoldSite = Seg1(0x300);      // cmp word [0x7A5E], 100: iterations the caption draws for
    public static readonly ExeAddress CaptionYSite = Seg1(0x2A0);      // mov word [bp-0x152], 180: the slide's starting y
    public static readonly ExeAddress CaptionSlideFromSite = Seg1(0x32D); // cmp word [0x7A5E], 70: iteration the slide starts on
    public static readonly ExeAddress CaptionSlidePxSite = Seg1(0x338);   // add word [bp-0x152], 3: px gained per iteration
    public static readonly ExeAddress CaptionCenterXSite = Seg1(0x33D);   // push 160: the x the caption is centered on
    public static readonly ExeAddress CardHoldSite = Seg1(0x55F);         // cmp word [bp-2], 79: iterations the card holds
    public static readonly ExeAddress IntroTickSite = Seg1(0x373);        // cmp ax, 8: the .ANI player's per-frame tick wait
    public static readonly ExeAddress CardTickSite = Seg1(0x53A);         // cmp ax, 6: the card's own hold loop
    public static readonly ExeAddress TitleTickSite = Seg1(0x2888);       // cmp ax, 6, but under jbe, so the title waits 7

    // ---- Ending sequence (seg2:0x1BF3) -------------------------------------

    /// <summary>Head of the ending's Pascal string table (seg2:0x192F): its tune name, then the animation
    /// file name, then the captions.</summary>
    public static readonly ExeAddress EndSequenceTuneName = Seg2(0x192F);

    /// <summary>Where that same string table's enemy roster starts (seg2:0x1964) (the gallery's species names
    /// and their mock-Latin subtitles, walked as one run of Pascal strings).</summary>
    public static readonly ExeAddress EnemyNameTable = Seg2(0x1964);

    /// <summary>Total species and subtitle entries in the ending roster table.</summary>
    public const int EnemyNameCount = 44;

    // ---- Node-info table (DGROUP) ------------------------------------------

    /// <summary>The node-info table (<c>DS:0x27A</c>): 5 maps × 16 records of
    /// <c>u16 sign; pascal name; u16 dest_map@23; u16 dest_node@25</c>.</summary>
    public static readonly ExeAddress NodeTable = DGroup(0x27A);

    /// <summary>Bytes per node-info record.</summary>
    public const int NodeRecordSize = 27;

    /// <summary>Node records per map, and maps in the table.</summary>
    public const int NodesPerMap = 16, MapCount = 5;

    // ---- Minigame prize names (DGROUP) --------------------------------------

    /// <summary>The bonus-prize short-name table (DS:0xED2) containing 5 fixed-stride records.</summary>
    public static readonly ExeAddress PrizeNameShortTable = DGroup(0xED2);

    /// <summary>The bonus-prize long-name table (DS:0xF12) containing 5 fixed-stride records.</summary>
    public static readonly ExeAddress PrizeNameLongTable = DGroup(0xF12);

    /// <summary>Entries in each prize-name table.</summary>
    public const int PrizeNameCount = 5;

    /// <summary>Bytes per record: length byte plus an 8-byte name buffer, in <see cref="PrizeNameShortTable"/>.</summary>
    public const int PrizeNameShortStride = 9;

    /// <summary>Bytes per record: length byte plus a 12-byte name buffer, in <see cref="PrizeNameLongTable"/>.</summary>
    public const int PrizeNameLongStride = 13;

    /// <summary>Head of the packed Pascal string chain for Find It board labels (seg3:0xCE21).</summary>
    public static readonly ExeAddress FindItStartLabel = Seg3(0xCE21);

    /// <summary>Pascal string for Stop It turning-reel label "?" (seg3:0xC1C3).</summary>
    public static readonly ExeAddress StopItSpinningLabel = Seg3(0xC1C3);

    /// <summary>Pascal string for Stop It focused-reel label "Stop It!" (seg3:0xC1C5).</summary>
    public static readonly ExeAddress StopItFocusedLabel = Seg3(0xC1C5);

    /// <summary>Ramp style operand for Stop It focused reel (seg3:0xC40B).</summary>
    public static readonly ExeAddress StopItFocusedStyleSite = Seg3(0xC40B);

    /// <summary>Ramp style operand for Stop It unfocused reels (seg3:0xC414).</summary>
    public static readonly ExeAddress StopItUnfocusedStyleSite = Seg3(0xC414);

    // ---- Slab screens: Instructions/Credits (DGROUP, seg1) ------------------

    /// <summary>The slab screens static configuration table (DS:0x10) for Instructions and Credits screens.</summary>
    public static readonly ExeAddress SlabConfigTable = DGroup(0x10);

    /// <summary>Words read from <see cref="SlabConfigTable"/>: max slab index for Instructions, max slab
    /// index for Credits, canvas width, canvas height, parallax set number.</summary>
    public const int SlabConfigWordCount = 5;

    /// <summary>The "Slab 1 of " counter template Pascal string (seg1:0x203F).</summary>
    public static readonly ExeAddress SlabCounterTemplate = Seg1(0x203F);

    // ---- Level-select screen music (seg2) -----------------------------------

    /// <summary>The <c>mov ax, [World]</c> the level-select screen's tune branch hangs off (seg2:0x120E).</summary>
    public static readonly ExeAddress WorldMapTuneWorldLoad = Seg2(0x120E);

    /// <summary>Bytes read at <see cref="WorldMapTuneWorldLoad"/>: <c>mov ax, word ptr [0x129E]</c>.</summary>
    public static readonly byte[] MovAxWorld = [0xA1, 0x9E, 0x12];

    /// <summary>The branch's first <c>cmp ax, &lt;world&gt;</c> case (seg2:0x1211).</summary>
    public static readonly ExeAddress WorldMapTuneFirstCase = Seg2(0x1211);

    /// <summary>Bytes from one case of that branch to the next, the last of them followed by the
    /// <c>mov di, &lt;tune name&gt;</c> every other world falls through to.</summary>
    public const int WorldMapTuneCaseStride = 17;

    /// <summary>Worlds the branch tests for before that fall-through.</summary>
    public const int WorldMapTuneCaseCount = 2;
}
