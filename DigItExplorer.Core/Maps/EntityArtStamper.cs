using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Core.Maps;

/// <summary>Stamps decoded sprite art for entities onto composed RGB map canvases.</summary>
internal static class EntityArtStamper
{
    // category -> (sheet base name, flood-fill separator [null = the sheet's own (0,0) pixel], cell picker).
    private static readonly Dictionary<byte, (string Sheet, byte? Separator, Func<DlfRecord, int> Pick)> Stamps = new()
    {
        [0x5C] = ("WO_PLANT", (byte)224, r => r.Type),
        [0x00] = ("WO_G&S", null, r => r.Type),              // gold item, the sheet's first row
        [0x01] = ("WO_G&S", null, r => 6 + r.Type),          // silver item, the second row of that same sheet
        [0x02] = ("WO_GEMS", null, r => r.Type),
    };

    /// <summary>Stamps static entity categories (plants, gold, silver, gems) onto the canvas.</summary>
    /// <param name="rgb">Row-major 24-bit RGB canvas buffer.</param>
    /// <param name="alpha">Row-major coverage plane marking which canvas pixels art has painted.</param>
    /// <param name="width">Canvas width in pixels.</param>
    /// <param name="height">Canvas height in pixels.</param>
    /// <param name="records">Level DLF entity records.</param>
    /// <param name="palette">VGA palette used to color sprite indices.</param>
    /// <param name="loadSheet">Resolver function for sprite sheet archive bytes.</param>
    /// <returns>Set of record indices that were stamped.</returns>
    public static HashSet<int> Stamp(byte[] rgb, byte[] alpha, int width, int height, IReadOnlyList<DlfRecord> records,
        VgaPalette palette, Func<string, byte[]?> loadSheet)
    {
        var stampedIndices = new HashSet<int>();
        var sliceCache = new Dictionary<string, IReadOnlyList<SpriteCell>>();

        // Plants are a foreground decoration drawn in front of all other static items.
        var order = Enumerable.Range(0, records.Count).OrderBy(i => records[i].Category == 0x5C ? 1 : 0);

        foreach (var i in order)
        {
            var rec = records[i];
            if (!Stamps.TryGetValue(rec.Category, out var stamp)) continue;
            if (rec.X >= width || rec.Y >= height) continue; // sits outside the cropped playfield

            if (!sliceCache.TryGetValue(stamp.Sheet, out var cells))
            {
                var sheetBytes = loadSheet($"{stamp.Sheet}.SPF");
                if (sheetBytes is null)
                {
                    sliceCache[stamp.Sheet] = [];
                    continue;
                }
                var frame = SheetImage.Read(sheetBytes).Frames[0];
                byte separator = stamp.Separator ?? frame[0];
                cells = SpriteSheetSlicer.Slice(frame, separator);
                sliceCache[stamp.Sheet] = cells;
            }

            int idx = stamp.Pick(rec);
            if (idx < 0 || idx >= cells.Count) continue;

            StampSprite(rgb, alpha, width, height, rec.X, rec.Y, cells[idx], palette, mirrored: rec.P0 == 0xFFFF);
            stampedIndices.Add(i);
        }

        return stampedIndices;
    }

    // Which grid cell stands in for a species on the map, as zero-based (row, col), where its frame 0 is a
    // poor likeness: Slugger's frame 0 is shell-retracted. A category absent here uses cell 0.
    private static readonly Dictionary<byte, (int Row, int Col)> RestFrameRc = new()
    {
        [0x06] = (1, 5), // Slugger
        [0x0C] = (1, 5), // Spurk
    };

    /// <summary>Stamps grid-based enemy species sprites onto the canvas using rest poses.</summary>
    public static HashSet<int> StampEnemies(byte[] rgb, byte[] alpha, int width, int height, IReadOnlyList<DlfRecord> records,
        VgaPalette palette, Func<string, byte[]?> loadWorldSheet)
    {
        var stampedIndices = new HashSet<int>();
        var frameCache = new Dictionary<string, byte[]?>();

        for (int i = 0; i < records.Count; i++)
        {
            var rec = records[i];
            if (!EnemyGrids.TryGet(rec.Category, out var grid)) continue;
            if (rec.X >= width || rec.Y >= height) continue;

            if (!frameCache.TryGetValue(grid.Sheet, out var frame))
            {
                var sheetBytes = loadWorldSheet(grid.Sheet);
                frame = sheetBytes is null ? null : SheetImage.Read(sheetBytes).Frames[0];
                frameCache[grid.Sheet] = frame;
            }
            if (frame is null) continue;

            var (row, col) = RestFrameRc.TryGetValue(rec.Category, out var rc) ? rc : (0, 0);
            var cell = GridCell(frame, grid.Cols, grid.StrideX, grid.StrideY, grid.W, grid.H, row * grid.Cols + col);
            if (cell is null) continue;

            StampSprite(rgb, alpha, width, height, rec.X, rec.Y, cell.Value, palette, mirrored: rec.P0 == 0xFFFF);
            stampedIndices.Add(i);
        }

        return stampedIndices;
    }

    // (page, x0, y0, w, h): a literal rect crop from that page of the chained-frame sheet. Each species is
    // one fixed rect rather than a cell of a uniform grid, which is why no stride appears here.
    private static readonly (int MpfFrame, int X0, int Y0, int W, int H)[] FishGrids =
    [
        (0, 0, 0,   34, 20), // 0 Rockerfish
        (0, 0, 42,  39, 34), // 1 Sea Draggo
        (0, 0, 112, 31, 14), // 2 Hopperfish
        (0, 0, 142, 32, 18), // 3 Aqua Slugger
        (1, 0, 0,   23, 11), // 4 Nirpies
        (1, 0, 24,  34, 20), // 5 Sea Spurk
    ];

    private static readonly (int MpfFrame, int X0, int Y0, int W, int H)[] GhostGrids =
    [
        (0, 0, 0,   32, 19), // 0 Ghost Slugger
        (0, 0, 60,  38, 30), // 1 Ghost Draggo
        (0, 0, 153, 29, 25), // 2 Ghost Rocker (rows 1+ on page 1)
        (1, 0, 52,  42, 36), // 3 Ghost Pyrosaur
    ];

    /// <summary>Stamps aquatic and ghost enemy species sprites from fixed sheet rectangles.</summary>
    public static HashSet<int> StampSpeciesGrids(byte[] rgb, byte[] alpha, int width, int height, IReadOnlyList<DlfRecord> records,
        VgaPalette palette, Func<string, byte[]?> loadSheet)
    {
        var stampedIndices = new HashSet<int>();
        var frameCache = new Dictionary<string, IReadOnlyList<byte[]>?>();

        for (int i = 0; i < records.Count; i++)
        {
            var rec = records[i];
            string sheet;
            (int MpfFrame, int X0, int Y0, int W, int H)[] table;
            if (rec.Category == 0x12) { sheet = "WO_FISH"; table = FishGrids; }
            else if (rec.Category == 0x13) { sheet = "WO_GHOST"; table = GhostGrids; }
            else continue;

            if (rec.Type >= table.Length) continue;
            if (rec.X >= width || rec.Y >= height) continue;

            if (!frameCache.TryGetValue(sheet, out var frames))
            {
                var sheetBytes = loadSheet(sheet);
                frames = sheetBytes is null ? null : SheetImage.Read(sheetBytes).Frames;
                frameCache[sheet] = frames;
            }
            if (frames is null) continue;

            var (mf, x0, y0, w, h) = table[rec.Type];
            if (mf >= frames.Count) continue;

            var cell = SpriteSheetSlicer.RectCell(frames[mf], x0, y0, x0 + w - 1, y0 + h - 1);
            StampSprite(rgb, alpha, width, height, rec.X, rec.Y, cell, palette, mirrored: rec.P0 == 0xFFFF);
            stampedIndices.Add(i);
        }

        return stampedIndices;
    }

    // The standing pose the land spawn is drawn in, within the world-costumed Dug sheet.
    private const int DugSpawnX1 = 128, DugSpawnY1 = 38, DugSpawnX2 = 159, DugSpawnY2 = 73;

    // One fixed pose, the same whatever the record's own type says, unlike every other category here.
    private const int BossGridCols = 3, BossStrideX = 95, BossStrideY = 72, BossW = 94, BossH = 71, BossCell = 15;

    /// <summary>Stamps player spawn and boss sprites at simulated landing positions.</summary>
    public static HashSet<int> StampSpawns(byte[] rgb, byte[] alpha, int width, int height, IReadOnlyList<DlfRecord> records,
        VgaPalette palette, Func<byte[]?> loadDugSpawn, Func<byte[]?> loadWoBoss)
    {
        var stampedIndices = new HashSet<int>();

        for (int i = 0; i < records.Count; i++)
        {
            var rec = records[i];
            if (rec.Category != LandingSimulator.CatPlayer && rec.Category != 0x32) continue;
            if (rec.X >= width || rec.Y >= height) continue;

            SpriteCell? cell = rec.Category == LandingSimulator.CatPlayer
                ? DugSpawnCell(loadDugSpawn)
                : BossCellFrom(loadWoBoss);
            if (cell is null) continue;

            StampSprite(rgb, alpha, width, height, rec.X, rec.Y, cell.Value, palette, mirrored: rec.P0 == 0xFFFF);
            stampedIndices.Add(i);
        }

        return stampedIndices;
    }

    private static SpriteCell? DugSpawnCell(Func<byte[]?> loadDugSpawn)
    {
        var sheetBytes = loadDugSpawn();
        if (sheetBytes is null) return null;
        var frame = SheetImage.Read(sheetBytes).Frames[0];
        return SpriteSheetSlicer.RectCell(frame, DugSpawnX1, DugSpawnY1, DugSpawnX2, DugSpawnY2);
    }

    private static SpriteCell? BossCellFrom(Func<byte[]?> loadWoBoss)
    {
        var sheetBytes = loadWoBoss();
        if (sheetBytes is null) return null;
        var frames = SheetImage.Read(sheetBytes).Frames;
        int page = BossCell / 6, rem = BossCell % 6;
        return page >= frames.Count ? null : GridCell(frames[page], BossGridCols, BossStrideX, BossStrideY, BossW, BossH, rem);
    }

    private const byte MvlSeparator = 255; // same WO_MVL flood-fill separator ColliderStamper uses

    // Open-entrance graphic rectangle on WO_DRAIN (bottom-left cell of 2x2 grid).
    private const int DrainRectX1 = 1, DrainRectY1 = 52, DrainRectX2 = 89, DrainRectY2 = 101;

    // Fixed crop rectangle on GENERAL sheet for the water-level exit sign.
    private const int ExitSignX1 = 0, ExitSignY1 = 123, ExitSignX2 = 27, ExitSignY2 = 149;

    /// <summary>Stamps mechanism sprites (moving platforms, falling rocks, exit signs, drains) onto canvas.</summary>
    public static HashSet<int> StampMechanisms(byte[] rgb, byte[] alpha, int width, int height, IReadOnlyList<DlfRecord> records,
        VgaPalette palette, Func<string, byte[]?> loadWorldSheet, Func<string, byte[]?> loadSheet)
    {
        var stampedIndices = new HashSet<int>();
        IReadOnlyList<SpriteCell>? mvlGraphics = null;
        bool mvlLoaded = false;
        SpriteCell? drainCell = null;
        bool drainLoaded = false;
        SpriteCell? exitCell = null;
        bool exitLoaded = false;
        SpriteCell? rckCell = null;
        bool rckLoaded = false;

        for (int i = 0; i < records.Count; i++)
        {
            var rec = records[i];
            if (rec.X >= width || rec.Y >= height) continue;

            SpriteCell? cell;
            switch (rec.Category)
            {
                case 0x04:
                    if (!mvlLoaded)
                    {
                        mvlLoaded = true;
                        var sheetBytes = loadWorldSheet("WO_MVL");
                        if (sheetBytes is not null)
                        {
                            var frame = SheetImage.Read(sheetBytes).Frames[0];
                            mvlGraphics = SpriteSheetSlicer.Slice(frame, MvlSeparator)
                                .Select(SpriteSheetSlicer.TopHalf).ToArray();
                        }
                    }
                    cell = mvlGraphics is not null && rec.Type < mvlGraphics.Count ? mvlGraphics[rec.Type] : null;
                    break;

                case 0x5B:
                    if (!drainLoaded)
                    {
                        drainLoaded = true;
                        var sheetBytes = loadSheet("WO_DRAIN.SPF");
                        if (sheetBytes is not null)
                        {
                            var frame = SheetImage.Read(sheetBytes).Frames[0];
                            drainCell = SpriteSheetSlicer.RectCell(frame, DrainRectX1, DrainRectY1, DrainRectX2, DrainRectY2);
                        }
                    }
                    cell = drainCell;
                    break;

                case 0x5A:
                    if (!exitLoaded)
                    {
                        exitLoaded = true;
                        var sheetBytes = loadSheet("GENERAL.SPF");
                        if (sheetBytes is not null)
                        {
                            var frame = SheetImage.Read(sheetBytes).Frames[0];
                            exitCell = SpriteSheetSlicer.RectCell(frame, ExitSignX1, ExitSignY1, ExitSignX2, ExitSignY2);
                        }
                    }
                    cell = exitCell;
                    break;

                case 0x14:
                    if (!rckLoaded)
                    {
                        rckLoaded = true;
                        var sheetBytes = loadWorldSheet("WO_RCK");
                        if (sheetBytes is not null)
                        {
                            var frame = SheetImage.Read(sheetBytes).Frames[0];
                            var cells = SpriteSheetSlicer.Slice(frame, frame[0]);
                            if (cells.Count > 0)
                                rckCell = cells[0];
                        }
                    }
                    cell = rckCell;
                    break;

                default:
                    continue;
            }

            if (cell is null) continue;
            StampSprite(rgb, alpha, width, height, rec.X, rec.Y, cell.Value, palette, mirrored: rec.P0 == 0xFFFF);
            stampedIndices.Add(i);
        }

        return stampedIndices;
    }

    /// <summary>Crops a sprite cell from a uniform grid layout on a decoded sheet frame.</summary>
    private static SpriteCell? GridCell(byte[] frame, int cols, int strideX, int strideY, int w, int h, int frameIndex)
    {
        int x0 = (frameIndex % cols) * strideX, y0 = (frameIndex / cols) * strideY;
        if (x0 + w > FrameCodec.Width || y0 + h > FrameCodec.Height) return null;

        var pixels = new byte[w * h];
        for (int yy = 0; yy < h; yy++)
            Array.Copy(frame, (y0 + yy) * FrameCodec.Width + x0, pixels, yy * w, w);
        return new SpriteCell(x0, y0, w, h, pixels);
    }

    private static void StampSprite(byte[] rgb, byte[] alpha, int canvasW, int canvasH, int x0, int y0, SpriteCell cell,
        VgaPalette palette, bool mirrored)
    {
        var pal = palette.Rgb;
        foreach (var (dst, v) in new ClippedBlit(cell, x0, y0, canvasW, canvasH, mirrored))
        {
            if (v == 0) continue; // palette index 0 = transparent

            RgbCanvas.PaintIndex(rgb, alpha, dst, pal, v);
        }
    }

    // The water-level player spawn's rest pose. The sheet is a swim-animation grid, but only its first cell
    // is ever wanted here, so the rect is written out rather than indexed.
    private const int Dug7aX1 = 0, Dug7aY1 = 0, Dug7aX2 = 46, Dug7aY2 = 33;

    /// <summary>Stamps dig-spot X markers and water-level player spawn sprites.</summary>
    public static HashSet<int> StampMarkers(byte[] rgb, byte[] alpha, int width, int height, IReadOnlyList<DlfRecord> records,
        VgaPalette palette, string levelStem, Func<string, byte[]?> loadSheet)
    {
        var stampedIndices = new HashSet<int>();
        bool underworld = levelStem.StartsWith("LVL6", StringComparison.Ordinal);

        IReadOnlyList<SpriteCell>? dareaCells = null;
        bool dareaLoaded = false;
        SpriteCell? dug7aCell = null;
        bool dug7aLoaded = false;

        for (int i = 0; i < records.Count; i++)
        {
            var rec = records[i];
            if (rec.X >= width || rec.Y >= height) continue;

            SpriteCell? cell;
            bool mirrored;
            if (rec.Category == 0x05)
            {
                if (!dareaLoaded)
                {
                    dareaLoaded = true;
                    var sheetBytes = loadSheet("WO_DAREA.SPF");
                    if (sheetBytes is not null)
                    {
                        var frame = SheetImage.Read(sheetBytes).Frames[0];
                        dareaCells = SpriteSheetSlicer.Slice(frame, frame[0]);
                    }
                }
                int idx = DareaCellIndex(rec, underworld);
                cell = dareaCells is not null && idx < dareaCells.Count ? dareaCells[idx] : null;
                mirrored = false; // 0x05 is never mirrored
            }
            else if (rec.Category == 0x63)
            {
                if (!dug7aLoaded)
                {
                    dug7aLoaded = true;
                    var sheetBytes = loadSheet("DUG7A.SPF");
                    if (sheetBytes is not null)
                    {
                        var frame = SheetImage.Read(sheetBytes).Frames[0];
                        dug7aCell = SpriteSheetSlicer.RectCell(frame, Dug7aX1, Dug7aY1, Dug7aX2, Dug7aY2);
                    }
                }
                cell = dug7aCell;
                mirrored = rec.P0 == 0xFFFF;
            }
            else continue;

            if (cell is null) continue;
            StampSprite(rgb, alpha, width, height, rec.X, rec.Y, cell.Value, palette, mirrored);
            stampedIndices.Add(i);
        }

        return stampedIndices;
    }

    /// <summary>Selects the dig-spot sprite cell index based on record type and underworld status.</summary>
    private static int DareaCellIndex(DlfRecord rec, bool underworld)
        => rec.Type == 0 ? (underworld ? 3 : 0) : 2;
}
