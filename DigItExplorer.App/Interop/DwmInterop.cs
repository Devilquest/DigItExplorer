using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DigItExplorer.App.Interop;

/// <summary>Configures Desktop Window Manager (DWM) frame attributes for custom window chrome.</summary>
internal static class DwmInterop
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    // Win10 20H1+ / Win11 (attribute 20); Win10 1809-1909 used legacy attribute 19.
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY = 19;

    // Windows 11 (build 22000+) corner rounding preference; ignored on earlier OS versions.
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_DONOTROUND = 1;

    /// <summary>Enables immersive dark mode for the window's non-client frame and drop shadow.</summary>
    /// <param name="window">The target window.</param>
    public static void ApplyDarkTitleBar(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int enabled = 1;
        if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY, ref enabled, sizeof(int));
    }

    /// <summary>Disables Windows 11 DWM corner rounding to preserve rectangular window borders.</summary>
    /// <param name="window">The target window.</param>
    public static void SquareOffCorners(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int preference = DWMWCP_DONOTROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
    }
}
