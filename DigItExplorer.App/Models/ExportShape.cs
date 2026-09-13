namespace DigItExplorer.App.Models;

/// <summary>Structural category of exportable resource content.</summary>
internal enum ExportShape
{
    /// <summary>Single static image render.</summary>
    Still,

    /// <summary>Multi-frame sprite or texture sheet.</summary>
    SheetFrames,

    /// <summary>Scripted multi-step sprite animation.</summary>
    Animation,
}
