using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DigItExplorer.App.Interop;

/// <summary>Routes non-client hit testing and clicks for custom title bar maximize buttons to enable Windows 11 Snap Layouts.</summary>
internal static class CaptionButtonHitTest
{
    private const int WM_NCHITTEST = 0x0084;
    private const int WM_NCMOUSEMOVE = 0x00A0;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_NCLBUTTONUP = 0x00A2;
    private const int WM_NCMOUSELEAVE = 0x02A2;
    private const int HTMAXBUTTON = 9;

    private const int TME_LEAVE = 0x00000002;
    private const int TME_NONCLIENT = 0x00000010;

    [StructLayout(LayoutKind.Sequential)]
    private struct TrackMouseEventOptions
    {
        public int Size;
        public int Flags;
        public IntPtr TrackedWindow;
        public int HoverTimeout;
    }

    [DllImport("user32.dll")]
    private static extern bool TrackMouseEvent(ref TrackMouseEventOptions options);

    /// <summary>Attached property indicating whether the non-client maximize button is hovered.</summary>
    public static readonly DependencyProperty IsHoveredProperty =
        DependencyProperty.RegisterAttached("IsHovered", typeof(bool), typeof(CaptionButtonHitTest),
            new PropertyMetadata(false));

    /// <summary>Sets the attached <see cref="IsHoveredProperty"/> value.</summary>
    public static void SetIsHovered(DependencyObject element, bool value) => element.SetValue(IsHoveredProperty, value);

    /// <summary>Gets the attached <see cref="IsHoveredProperty"/> value.</summary>
    public static bool GetIsHovered(DependencyObject element) => (bool)element.GetValue(IsHoveredProperty);

    /// <summary>Hooks the window procedure to handle non-client hit-testing for the maximize button.</summary>
    /// <param name="window">The target window.</param>
    /// <param name="maximizeButton">The button element acting as the maximize control.</param>
    public static void Attach(Window window, FrameworkElement maximizeButton)
    {
        if (PresentationSource.FromVisual(window) is not HwndSource source) return;
        source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            WndProc(window, maximizeButton, hwnd, msg, wParam, lParam, ref handled));
    }

    private static IntPtr WndProc(Window window, FrameworkElement maximizeButton, IntPtr hwnd, int msg,
        IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_NCHITTEST:
            {
                // Updates hover state only on change to avoid redundant invalidation during mouse move.
                bool over = IsOverMaximizeButton(maximizeButton, lParam);
                if (over != GetIsHovered(maximizeButton)) SetIsHovered(maximizeButton, over);
                if (!over) break;
                handled = true;
                return (IntPtr)HTMAXBUTTON;
            }

            case WM_NCMOUSEMOVE when (int)wParam == HTMAXBUTTON:
                // Track WM_NCMOUSELEAVE to clear hover state when leaving the window via the button edge.
                var options = new TrackMouseEventOptions
                {
                    Size = Marshal.SizeOf<TrackMouseEventOptions>(),
                    Flags = TME_LEAVE | TME_NONCLIENT,
                    TrackedWindow = hwnd,
                    HoverTimeout = 0,
                };
                TrackMouseEvent(ref options);
                handled = true;
                return IntPtr.Zero;

            case WM_NCMOUSELEAVE:
                if (GetIsHovered(maximizeButton)) SetIsHovered(maximizeButton, false);
                break;

            case WM_NCLBUTTONDOWN when (int)wParam == HTMAXBUTTON:
                // Handled on button-up to prevent default window procedure from drawing system caption buttons.
                handled = true;
                return IntPtr.Zero;

            case WM_NCLBUTTONUP when (int)wParam == HTMAXBUTTON:
                handled = true;
                if (window.WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(window);
                else SystemCommands.MaximizeWindow(window);
                return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private static bool IsOverMaximizeButton(FrameworkElement maximizeButton, IntPtr lParam)
    {
        // Avoid PointToScreen during early layout or window disposal when visual is not connected.
        if (!maximizeButton.IsVisible || PresentationSource.FromVisual(maximizeButton) is null) return false;

        int x = unchecked((short)((long)lParam & 0xFFFF));
        int y = unchecked((short)(((long)lParam >> 16) & 0xFFFF));

        try
        {
            var topLeft = maximizeButton.PointToScreen(new Point(0, 0));
            var bottomRight = maximizeButton.PointToScreen(
                new Point(maximizeButton.ActualWidth, maximizeButton.ActualHeight));
            return x >= topLeft.X && x < bottomRight.X && y >= topLeft.Y && y < bottomRight.Y;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
