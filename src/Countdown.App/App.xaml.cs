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
}
