using System.IO;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace Countdown.App.Services;

/// <summary>
/// Registers explicitly sized HICONs on each window instead of relying on WPF's
/// frame auto-selection: the large IconSize frame (48px @150% DPI) for both
/// ICON_BIG and ICON_SMALL, so every consumer (taskbar, Alt+Tab, title bar)
/// downscales one high-resolution frame instead of upscaling a small one.
/// HICONs are loaded once and kept for the app lifetime: WM_SETICON does not
/// transfer ownership, and every window may share the same handles.
/// </summary>
public static class WindowIconService
{
    private const int WmSetIcon = 0x0080;
    private const int IconSmall = 0;
    private const int IconBig = 1;

    private static readonly object Sync = new();
    private static System.Drawing.Icon? _big;
    private static System.Drawing.Icon? _small;

    /// <summary>Call before the window is shown; icons are set once its HWND exists.</summary>
    public static void Apply(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            EnsureIcons();
            var hwnd = new WindowInteropHelper(window).Handle;
            PInvoke.SendMessage(hwnd, WmSetIcon, (nint)IconBig, _big!.Handle);
            // Same high-resolution frame for both slots: high-quality downscaling
            // (taskbar/title bar) beats upscaling a 16/24px HICON visibly.
            PInvoke.SendMessage(hwnd, WmSetIcon, (nint)IconSmall, _small!.Handle);
        };
    }

    private static void EnsureIcons()
    {
        lock (Sync)
        {
            if (_big is not null)
            {
                return;
            }

            var resource = Application.GetResourceStream(new Uri("/Assets/app.ico", UriKind.Relative))
                ?? throw new FileNotFoundException("Window icon resource not found: /Assets/app.ico");
            using var source = resource.Stream;

            // Taskbar/title-bar slot. Using the larger IconSize frame for both
            // slots (rather than SmallIconSize) means the shell always downscales;
            // the Windows 11 taskbar normalizes the result either way, but this
            // stays sharp on older shells and other DPI/monitor combinations.
            var size = Forms.SystemInformation.IconSize;   // 32px @100%, 48px @150%
            _big = new System.Drawing.Icon(source, size.Width, size.Height);

            source.Position = 0;
            _small = new System.Drawing.Icon(source, size.Width, size.Height);
        }
    }

    private static class PInvoke
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);
    }
}
