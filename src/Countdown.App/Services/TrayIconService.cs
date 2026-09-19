using System.IO;
using System.Windows;
using Countdown.App.ViewModels;
using Forms = System.Windows.Forms;
using MenuItem = System.Windows.Forms.ToolStripMenuItem;

namespace Countdown.App.Services;

/// <summary>
/// Notification-area (system tray) icon. Its menu mirrors the floating
/// window's context menu; double-click toggles the window. The icon stays
/// alive for the whole app lifetime and is disposed on exit so no ghost
/// icon lingers in the tray.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly Window _window;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly MenuItem _toggleItem;
    private readonly Stream _iconStream;
    // WinForms menus don't participate in the WPF CommandManager; we keep the
    // command bindings so we can re-evaluate CanExecute when the menu opens.
    private readonly List<(MenuItem Item, System.Windows.Input.ICommand Command, object? Parameter)> _commands = new();
    private bool _disposed;

    public TrayIconService(Window window, TimerViewModel viewModel)
    {
        _window = window;

        // Load the same multi-size ico used by the window/exe from the
        // WPF resource pack; request the shell's small-icon size for crispness.
        var resource = Application.GetResourceStream(new Uri("/Assets/app.ico", UriKind.Relative))
            ?? throw new FileNotFoundException("Tray icon resource not found: /Assets/app.ico");
        _iconStream = resource.Stream;
        var size = Forms.SystemInformation.SmallIconSize;
        using var sourceIcon = new System.Drawing.Icon(_iconStream, size.Width, size.Height);

        _toggleItem = new MenuItem();
        _toggleItem.Click += (_, _) => ToggleWindow();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.AddRange(new Forms.ToolStripItem[]
        {
            BoundItem("5 分钟", viewModel.StartPresetCommand, 5),
            BoundItem("10 分钟", viewModel.StartPresetCommand, 10),
            BoundItem("25 分钟", viewModel.StartPresetCommand, 25),
            BoundItem("自定义…", viewModel.StartCustomCommand),
            new Forms.ToolStripSeparator(),
            BoundItem("暂停", viewModel.PauseCommand),
            BoundItem("继续", viewModel.ResumeCommand),
            BoundItem("重置", viewModel.ResetCommand),
            new Forms.ToolStripSeparator(),
            _toggleItem,
            new Forms.ToolStripSeparator(),
            BoundItem("关于 Countdown", viewModel.AboutCommand),
            BoundItem("退出", viewModel.ExitCommand),
        });
        menu.Opening += (_, _) => SyncMenu();

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = (System.Drawing.Icon)sourceIcon.Clone(),
            Text = "Countdown",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _notifyIcon.DoubleClick += (_, _) => ToggleWindow();
    }

    private MenuItem BoundItem(string text, System.Windows.Input.ICommand command, object? parameter = null)
    {
        var item = new MenuItem(text);
        item.Click += (_, _) => command.Execute(parameter);
        _commands.Add((item, command, parameter));
        return item;
    }

    // Refresh enabled states and the show/hide label each time the menu opens.
    private void SyncMenu()
    {
        foreach (var (item, command, parameter) in _commands)
        {
            item.Enabled = command.CanExecute(parameter);
        }

        _toggleItem.Text = _window.IsVisible ? "隐藏悬浮窗" : "显示悬浮窗";
    }

    private void ToggleWindow()
    {
        if (_window.IsVisible)
        {
            _window.Hide();
        }
        else
        {
            _window.Show();
            _window.Activate();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _iconStream.Dispose();
    }
}
