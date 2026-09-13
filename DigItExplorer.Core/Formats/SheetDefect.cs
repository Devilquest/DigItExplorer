namespace DigItExplorer.Core.Formats;

/// <summary>Structural defects detected when reading sheet files.</summary>
public enum SheetDefect
{
    /// <summary>The sheet file decoded without structural defects.</summary>
    None,

    /// <summary>Too short to hold even a header and a palette, so there is no sheet here to read.</summary>
    NotASheet,

    /// <summary>The header declares more frames than the file size allows.</summary>
    ImpossibleFrameCount,

    /// <summary>A frame length extends past the end of the file.</summary>
    TruncatedChain,

    /// <summary>A frame payload contains invalid or out-of-bounds compression opcodes.</summary>
    UndecodableFrame
}
