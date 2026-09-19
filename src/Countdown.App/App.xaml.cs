using System.Threading;
using System.Windows;
using Countdown.App.Services;
using Countdown.App.ViewModels;
using Countdown.App.Views;
using Countdown.Core;

namespace Countdown.App;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\Countdown-SingleInstance";

    static App()
    {
        // Runs before any WPF HWND is created. WinForms-enabled builds strip DPI
        // settings from the manifest, so opt into PerMonitorV2 explicitly here:
        // each monitor renders at native DPI instead of DWM bitmap-stretching.
        if (!PInvoke.SetProcessDpiAwarenessContext(new IntPtr(-4)) /* DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 */)
        {
            if (PInvoke.SetProcessDpiAwareness(2) != 0 /* PROCESS_PER_MONITOR_DPI_AWARE */)
            {
                PInvoke.SetProcessDPIAware();  // Vista/7 fallback
            }
        }
    }

    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;
    private PowerWatcher? _powerWatcher;
    private TrayIconService? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName,
            createdNew: out var createdNew);
        if (!createdNew)
        {
            // Another instance owns the mutex: exit immediately, do not touch its resources.
            Shutdown();
            return;
        }

        _ownsMutex = true;

        var clock = new SystemClock();
        var settingsStore = new JsonSettingsStore();
        var placement = new WindowPlacementService(settingsStore);
        _powerWatcher = new PowerWatcher();

        var session = new CountdownSession(clock);
        var viewModel = new TimerViewModel(session);

        var window = new TimerWindow { DataContext = viewModel };
        WindowIconService.Apply(window);
        placement.Apply(window);
        window.Closing += (_, _) => placement.Save(window);

        _powerWatcher.Resumed += (_, _) =>
            Current.Dispatcher.BeginInvoke(() => viewModel.HandleResumed());

        _trayIcon = new TrayIconService(window, viewModel);

        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _powerWatcher?.Dispose();
        if (_ownsMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static class PInvoke
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [System.Runtime.InteropServices.DllImport("shcore.dll")]
        public static extern int SetProcessDpiAwareness(int value);
    }
}
