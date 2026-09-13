using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DigItExplorer.App.Interop;

/// <summary>Handles <c>WM_GETMINMAXINFO</c> to constrain maximized window bounds to the monitor work area.</summary>
internal static class MaximizeBounds
{
    private const int WM_GETMINMAXINFO = 0x0024;
    private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point32
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point32 Reserved;
        public Point32 MaxSize;
        public Point32 MaxPosition;
        public Point32 MinTrackSize;
        public Point32 MaxTrackSize;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    /// <summary>Hooks the window procedure to clamp maximized window dimensions to the monitor work area.</summary>
    /// <param name="window">The target window.</param>
    public static void Attach(Window window)
    {
        if (PresentationSource.FromVisual(window) is not HwndSource source) return;
        source.AddHook(WndProc);
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_GETMINMAXINFO) return IntPtr.Zero;

        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero) return IntPtr.Zero;

        var monitorInfo = new MonitorLayout.MonitorInfo { Size = Marshal.SizeOf<MonitorLayout.MonitorInfo>() };
        if (!MonitorLayout.GetMonitorInfo(monitor, ref monitorInfo)) return IntPtr.Zero;

        var info = Marshal.PtrToStructure<MinMaxInfo>(lParam);

        // Position is relative to monitor origin so secondary monitors at negative coordinates align correctly.
        info.MaxPosition.X = monitorInfo.Work.Left - monitorInfo.Monitor.Left;
        info.MaxPosition.Y = monitorInfo.Work.Top - monitorInfo.Monitor.Top;
        info.MaxSize.X = monitorInfo.Work.Right - monitorInfo.Work.Left;
        info.MaxSize.Y = monitorInfo.Work.Bottom - monitorInfo.Work.Top;

        Marshal.StructureToPtr(info, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }
}
