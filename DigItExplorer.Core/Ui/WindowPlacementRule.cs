namespace DigItExplorer.Core.Ui;

/// <summary>Platform-agnostic rectangle representing window bounds in device-independent units.</summary>
public readonly record struct WindowRect(double Left, double Top, double Width, double Height)
{
    /// <summary>Right edge X coordinate.</summary>
    public double Right => Left + Width;

    /// <summary>Bottom edge Y coordinate.</summary>
    public double Bottom => Top + Height;

    /// <summary>Checks whether another rectangle is completely contained within this one.</summary>
    public bool Contains(WindowRect other)
        => other.Left >= Left && other.Top >= Top && other.Right <= Right && other.Bottom <= Bottom;
}

/// <summary>Validation and placement rules for restoring saved window position and dimensions.</summary>
public static class WindowPlacementRule
{
    /// <summary>Minimum title bar horizontal overlap required to consider a window grabbable.</summary>
    public const double MinimumGrabbableWidth = 100;

    /// <summary>Resolves a valid on-screen rectangle from saved settings and monitor bounds.</summary>
    /// <param name="saved">Saved window rectangle from settings, or null.</param>
    /// <param name="minWidth">Minimum declared window width.</param>
    /// <param name="minHeight">Minimum declared window height.</param>
    /// <param name="monitorWorkArea">Target monitor work area bounds, or null.</param>
    /// <param name="virtualScreen">Combined bounds of all active monitors.</param>
    /// <returns>Validated and clamped WindowRect, or null to use default centering.</returns>
    public static WindowRect? Resolve(WindowRect? saved, double minWidth, double minHeight,
        WindowRect? monitorWorkArea, WindowRect virtualScreen)
    {
        if (saved is not { } rect) return null;

        if (!double.IsFinite(rect.Left) || !double.IsFinite(rect.Top)) return null;
        if (!IsFinitePositive(rect.Width) || !IsFinitePositive(rect.Height)) return null;

        rect = rect with { Width = Math.Max(rect.Width, minWidth), Height = Math.Max(rect.Height, minHeight) };

        if (monitorWorkArea is not { } workArea) return null;

        var overlap = Math.Min(rect.Right, workArea.Right) - Math.Max(rect.Left, workArea.Left);
        if (overlap < MinimumGrabbableWidth) return null;
        if (rect.Top < workArea.Top) return null;

        if (!virtualScreen.Contains(rect))
        {
            var width = Math.Max(minWidth, Math.Min(rect.Width, workArea.Width));
            var height = Math.Max(minHeight, Math.Min(rect.Height, workArea.Height));
            var left = ClampPosition(rect.Left, workArea.Left, workArea.Right, width);
            var top = ClampPosition(rect.Top, workArea.Top, workArea.Bottom, height);
            rect = new WindowRect(left, top, width, height);
        }

        return rect;
    }

    private static bool IsFinitePositive(double value) => double.IsFinite(value) && value > 0;

    private static double ClampPosition(double value, double areaMin, double areaMax, double size)
    {
        var latestStart = areaMax - size;
        return latestStart < areaMin ? areaMin : Math.Clamp(value, areaMin, latestStart);
    }
}
