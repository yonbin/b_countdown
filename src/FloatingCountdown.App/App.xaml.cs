using System.Threading;
using System.Windows;
using FloatingCountdown.App.Services;
using FloatingCountdown.App.ViewModels;
using FloatingCountdown.App.Views;
using FloatingCountdown.Core;

namespace FloatingCountdown.App;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\FloatingCountdown-SingleInstance";

    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;
    private AudioAlertService? _audio;
    private PowerWatcher? _powerWatcher;

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
        _audio = new AudioAlertService();
        _powerWatcher = new PowerWatcher();

        var session = new CountdownSession(clock);
        var viewModel = new TimerViewModel(session, clock, _audio);

        var window = new TimerWindow { DataContext = viewModel };
        placement.Apply(window);
        window.Closing += (_, _) => placement.Save(window);

        _powerWatcher.Resumed += (_, _) =>
            Current.Dispatcher.BeginInvoke(() => viewModel.HandleResumed());

        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _powerWatcher?.Dispose();
        _audio?.Dispose();
        if (_ownsMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
