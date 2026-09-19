using System.Windows;
using Countdown.Core.Settings;

namespace Countdown.App.Services;

/// <summary>
/// Restores and persists the floating window position. Restored points that no
/// longer lie on any connected screen (e.g. a monitor was unplugged) fall back
/// to the primary screen's top-right corner.
/// </summary>
public sealed class WindowPlacementService
{
    private const double EdgeMargin = 16;

    private readonly ISettingsStore _store;

    public WindowPlacementService(ISettingsStore store)
    {
        _store = store;
    }

    // A restored window must keep at least this much of itself on a connected screen.
    private const double MinVisibleWidth = 60;
    private const double MinVisibleHeight = 40;

    public void Apply(Window window)
    {
        var settings = _store.Load();
        var virtualScreen = CurrentVirtualScreen();

        window.WindowStartupLocation = WindowStartupLocation.Manual;

        if (settings.WindowLeft is { } left && settings.WindowTop is { } top)
        {
            var restored = new Rect(left, top, window.Width, window.Height);
            var visible = Intersect(restored, virtualScreen);
            if (visible.Width >= MinVisibleWidth && visible.Height >= MinVisibleHeight)
            {
                window.Left = left;
                window.Top = top;
                return;
            }
        }

        // First run, or saved position is mostly off-screen (e.g. monitor unplugged).
        var workArea = SystemParameters.WorkArea;
        window.Left = workArea.Right - window.Width - EdgeMargin;
        window.Top = workArea.Top + EdgeMargin;
    }

    private static Rect Intersect(Rect a, Rect b)
    {
        var left = Math.Max(a.Left, b.Left);
        var top = Math.Max(a.Top, b.Top);
        var right = Math.Min(a.Right, b.Right);
        var bottom = Math.Min(a.Bottom, b.Bottom);
        return right >= left && bottom >= top
            ? new Rect(left, top, right - left, bottom - top)
            : Rect.Empty;
    }

    public void Save(Window window)
    {
        var screen = CurrentVirtualScreen();
        _store.Save(new AppSettings
        {
            WindowLeft = window.Left,
            WindowTop = window.Top,
            VirtualScreenLeft = screen.Left,
            VirtualScreenTop = screen.Top,
            VirtualScreenWidth = screen.Width,
            VirtualScreenHeight = screen.Height
        });
    }

    private static Rect CurrentVirtualScreen() => new(
        SystemParameters.VirtualScreenLeft,
        SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth,
        SystemParameters.VirtualScreenHeight);
}
