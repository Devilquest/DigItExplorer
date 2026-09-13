using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.Interop;

/// <summary>Provides monitor work area queries in device-independent pixels via Win32 interop.</summary>
internal static class MonitorLayout
{
    private const int MONITOR_DEFAULTTONULL = 0x00000000;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect32
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MonitorInfo
    {
        public int Size;
        public Rect32 Monitor;
        public Rect32 Work;
        public int Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref Rect32 rect, int flags);

    [DllImport("user32.dll")]
    internal static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    /// <summary>Gets the work area in device-independent pixels for the monitor overlapping the given rectangle.</summary>
    /// <param name="window">The window used to determine DPI scaling factors.</param>
    /// <param name="rect">The bounding rectangle in device-independent pixels.</param>
    /// <returns>The monitor work area bounds, or null if no monitor contains the rectangle.</returns>
    public static WindowRect? TryGetWorkArea(Window window, WindowRect rect)
    {
        if (PresentationSource.FromVisual(window) is not HwndSource source) return null;

        var toDevice = source.CompositionTarget.TransformToDevice;
        var fromDevice = source.CompositionTarget.TransformFromDevice;

        var physical = ToRect32(rect, toDevice);
        var monitor = MonitorFromRect(ref physical, MONITOR_DEFAULTTONULL);
        if (monitor == IntPtr.Zero) return null;

        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return null;

        return FromRect32(info.Work, fromDevice);
    }

    private static Rect32 ToRect32(WindowRect rect, Matrix toDevice)
    {
        var topLeft = toDevice.Transform(new Point(rect.Left, rect.Top));
        var bottomRight = toDevice.Transform(new Point(rect.Right, rect.Bottom));
        return new Rect32
        {
            Left = (int)Math.Round(topLeft.X),
            Top = (int)Math.Round(topLeft.Y),
            Right = (int)Math.Round(bottomRight.X),
            Bottom = (int)Math.Round(bottomRight.Y),
        };
    }

    private static WindowRect FromRect32(Rect32 rect, Matrix fromDevice)
    {
        var topLeft = fromDevice.Transform(new Point(rect.Left, rect.Top));
        var bottomRight = fromDevice.Transform(new Point(rect.Right, rect.Bottom));
        return new WindowRect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
    }
}
