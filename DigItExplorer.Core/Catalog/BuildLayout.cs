namespace DigItExplorer.Core.Catalog;

/// <summary>Turns a <c>seg:offset</c> address into a file offset in one recognized build of <c>MAIN.EXE</c>.</summary>
internal sealed class BuildLayout
{
    // The three code segments and DGROUP, by their number in the executable's own table.
    private const int Seg1Number = 1, Seg2Number = 2, Seg3Number = 3, DGroupNumber = 6;

    private const int SegmentCount = 7;

    /// <summary>A build this application's addresses describe, told apart by the length of the segment they
    /// differ in, and carrying the shift that turns one build's seg1 addresses into the other's.</summary>
    /// <param name="Seg1Length">The length seg1 has in this build.</param>
    /// <param name="ShiftFrom">The seg1 offset the shift starts applying at.</param>
    /// <param name="Shift">Bytes to add to a recorded seg1 offset at or above <paramref name="ShiftFrom"/>.</param>
    /// <param name="PlaysLogoPrologue">Whether this build carries the logo prologue at all.</param>
    private readonly record struct KnownBuild(int Seg1Length, int ShiftFrom, int Shift, bool PlaysLogoPrologue);

    // Only seg1 differs between the two, and it differs by an insertion, so a length identifies the build
    // and the same insertion gives the shift. Everything else, all four bases included, is read from the file.
    private static readonly KnownBuild[] KnownBuilds =
    [
        new(15755, 0, 0, PlaysLogoPrologue: false),       // the build every address in ExeLayout was derived against
        new(15981, 0x280A, 226, PlaysLogoPrologue: true), // the same, with a distributor logo spliced into the title routine
    ];

    private readonly NeSegment[] _segments;
    private readonly KnownBuild _build;

    private BuildLayout(NeSegment[] segments, KnownBuild build)
    {
        _segments = segments;
        _build = build;
    }

    /// <summary>Recognizes the build from the executable's own segment table, or reports that it is one whose
    /// addresses this application does not know.</summary>
    public static bool TryRecognize(GameExecutable exe, out BuildLayout layout)
    {
        layout = null!;
        if (!NeSegmentTable.TryRead(exe, out var segments) || segments.Length != SegmentCount) return false;

        foreach (var build in KnownBuilds)
        {
            if (segments[Seg1Number - 1].Length != build.Seg1Length) continue;

            layout = new BuildLayout(segments, build);
            return true;
        }

        return false;
    }

    /// <summary>Whether this build plays a distributor logo before the game's own title sequence.</summary>
    public bool PlaysLogoPrologue => _build.PlaysLogoPrologue;

    /// <summary>The file offset <paramref name="address"/> is at in this build.</summary>
    public int At(ExeAddress address)
    {
        int offset = address.Segment == ExeSegment.Seg1 && !address.BuildOwn && address.Offset >= _build.ShiftFrom
            ? address.Offset + _build.Shift
            : address.Offset;

        return _segments[NumberOf(address.Segment) - 1].Base + offset;
    }

    private static int NumberOf(ExeSegment segment) => segment switch
    {
        ExeSegment.Seg1 => Seg1Number,
        ExeSegment.Seg2 => Seg2Number,
        ExeSegment.Seg3 => Seg3Number,
        _ => DGroupNumber,
    };
}
