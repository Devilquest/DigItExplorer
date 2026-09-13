namespace DigItExplorer.Core.Catalog;

/// <summary>Which of <c>MAIN.EXE</c>'s segments an address is measured from.</summary>
internal enum ExeSegment
{
    /// <summary>The intro and title sequences.</summary>
    Seg1,

    /// <summary>The loaders and the ending sequence.</summary>
    Seg2,

    /// <summary>The text font and the minigame asset and string lists.</summary>
    Seg3,

    /// <summary>Globals, addressed as <c>DS:xxxx</c>.</summary>
    DGroup,
}

/// <summary>A <c>seg:offset</c> address, before a build decides where in the file that is.</summary>
/// <param name="Segment">The segment the offset is measured from.</param>
/// <param name="Offset">The offset within that segment.</param>
/// <param name="BuildOwn">Whether the offset is already in the offsets of the build it will be read in,
/// so that build's own shift has nothing to do to it.</param>
internal readonly record struct ExeAddress(ExeSegment Segment, int Offset, bool BuildOwn = false)
{
    /// <summary>An address this application carries, which a build's own shift applies to.</summary>
    public static ExeAddress Recorded(ExeSegment segment, int offset) => new(segment, offset);

    /// <summary>An address decoded from the executable's own bytes, which is already that build's own and
    /// must be used exactly as it was read.</summary>
    public static ExeAddress FromOperand(ExeSegment segment, int offset) => new(segment, offset, BuildOwn: true);

    /// <summary>An address in code only one build carries, recorded in that build's own offsets because no
    /// other build has the code for a shift to be measured from.</summary>
    public static ExeAddress InAddedCode(ExeSegment segment, int offset) => new(segment, offset, BuildOwn: true);

    /// <summary>The same address <paramref name="delta"/> bytes further into its own segment.</summary>
    public static ExeAddress operator +(ExeAddress address, int delta) => address with { Offset = address.Offset + delta };
}
