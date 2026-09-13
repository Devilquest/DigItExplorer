namespace DigItExplorer.Core.Catalog;

/// <summary>Specifies the recognition status of <c>MAIN.EXE</c> during game folder discovery.</summary>
public enum MainExeStatus
{
    /// <summary>Absent from the folder, or present and unreadable.</summary>
    Unreadable,

    /// <summary>The executable is present but does not match expected segment addresses.</summary>
    DifferentBuild,

    /// <summary>The build this application's addresses describe.</summary>
    Recognized
}
