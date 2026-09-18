using System.Windows.Input;
using System.Windows.Threading;
using FloatingCountdown.App.Services;
using FloatingCountdown.App.Views;
using FloatingCountdown.Core;

namespace FloatingCountdown.App.ViewModels;

/// <summary>A card in the floating display: just the big value (no unit label).</summary>
public sealed record CardPart(string Value);

/// <summary>
/// Adapts <see cref="CountdownSession"/> to the floating window.
/// The DispatcherTimer only redraws; time truth lives in the session's
/// absolute end instant.
/// </summary>
public sealed class TimerViewModel : System.ComponentModel.INotifyPropertyChanged
{
    private static readonly TimeSpan AlertAutoStopAfter = TimeSpan.FromMinutes(2);

    private readonly CountdownSession _session;
    private readonly IClock _clock;
    private readonly AudioAlertService _audio;
    private readonly DispatcherTimer _tickTimer;

    private bool _wasSounding;
    private DateTimeOffset? _alertStartedAt;

    public TimerViewModel(CountdownSession session, IClock clock, AudioAlertService audio)
    {
        _session = session;
        _clock = clock;
        _audio = audio;

        _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _tickTimer.Tick += (_, _) => OnTick();
        _tickTimer.Start();

        StartPresetCommand = new RelayCommand(
            p => StartDuration(TimeSpan.FromMinutes(ToInt(p))),
            _ => CanStart);
        StartCustomCommand = new RelayCommand(_ => StartCustom(), _ => CanStart);
        PauseCommand = new RelayCommand(Pause, () => State == SessionState.Running);
        ResumeCommand = new RelayCommand(Resume, () => State == SessionState.Paused);
        ResetCommand = new RelayCommand(Reset, () => State != SessionState.Idle);
        AcknowledgeCommand = new RelayCommand(
            Acknowledge,
            () => State == SessionState.Finished && _session.AlertState == AlertState.Sounding);
        ExitCommand = new RelayCommand(() => System.Windows.Application.Current.Shutdown());
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public SessionState State => _session.State;

    public bool CanStart => State is SessionState.Idle or SessionState.Finished;

    /// <summary>Cards shown in the floating window (2 for mm/ss, 3 for h/mm/ss).</summary>
    public IReadOnlyList<CardPart> Cards { get; private set; } = IdleCards;

    public bool ShowHours { get; private set; }

    public ICommand StartPresetCommand { get; }
    public ICommand StartCustomCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand AcknowledgeCommand { get; }
    public ICommand ExitCommand { get; }

    /// <summary>Called immediately after a system resume so the display/alert never waits for a tick.</summary>
    public void HandleResumed()
    {
        _session.Refresh();
        SyncAlert(forceRestartWindow: true);
        RaiseAll();
    }

    private void OnTick()
    {
        _session.Refresh();
        SyncAlert(forceRestartWindow: false);
        RaiseAll();
    }

    private void SyncAlert(bool forceRestartWindow)
    {
        var sounding = _session.AlertState == AlertState.Sounding;

        if (sounding && !_wasSounding)
        {
            _audio.Start();
            _alertStartedAt = _clock.UtcNow;
        }
        else if (forceRestartWindow && sounding)
        {
            // Woke up at/after the end: (re)start the 2-minute auto-stop window at wake time.
            _audio.Start();
            _alertStartedAt = _clock.UtcNow;
        }
        else if (!sounding && _wasSounding)
        {
            _audio.Stop();
            _alertStartedAt = null;
        }
        else if (sounding && _alertStartedAt is { } startedAt &&
                 _clock.UtcNow - startedAt >= AlertAutoStopAfter)
        {
            // Auto-stop after 2 minutes; the red 00:00 state remains.
            _audio.Stop();
            _session.Acknowledge();
        }

        _wasSounding = _session.AlertState == AlertState.Sounding;
    }

    private void StartDuration(TimeSpan duration)
    {
        if (!CanStart)
        {
            return;
        }

        _audio.Stop();
        _wasSounding = false;
        _alertStartedAt = null;
        _session.Start(duration);
        RaiseAll();
    }

    private void StartCustom()
    {
        if (!CanStart)
        {
            return;
        }

        var owner = System.Windows.Application.Current.MainWindow;
        var input = CustomDurationDialog.Prompt(owner);
        if (DurationParser.TryParse(input, out var duration))
        {
            StartDuration(duration);
        }
    }

    private void Pause()
    {
        _session.Pause();
        RaiseAll();
    }

    private void Resume()
    {
        _session.Resume();
        RaiseAll();
    }

    private void Reset()
    {
        _audio.Stop();
        _wasSounding = false;
        _alertStartedAt = null;
        _session.Reset();
        RaiseAll();
    }

    private void Acknowledge()
    {
        _session.Acknowledge();
        _audio.Stop();
        _alertStartedAt = null;
        RaiseAll();
    }

    private static readonly IReadOnlyList<CardPart> IdleCards =
        new[] { new CardPart("00"), new CardPart("00") };

    private static int ToInt(object? parameter) =>
        parameter is int i ? i : System.Convert.ToInt32(parameter);

    private void RaiseAll()
    {
        UpdateDisplay();
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(string.Empty));
        CommandManager.InvalidateRequerySuggested();
    }

    private void UpdateDisplay()
    {
        switch (State)
        {
            case SessionState.Idle:
                Cards = IdleCards;
                ShowHours = false;
                break;
            case SessionState.Finished:
                // Stays at 00:00; no text, no upward counting. The card color signals the end.
                Cards = new[] { new CardPart("00"), new CardPart("00") };
                ShowHours = false;
                break;
            default:
                (Cards, ShowHours) = BuildCards(_session.Remaining!.Value);
                break;
        }
    }

    private static (IReadOnlyList<CardPart> Cards, bool ShowHours) BuildCards(TimeSpan t)
    {
        // Remaining rounds up (never shows 00 before the end).
        var totalSeconds = (int)Math.Ceiling(t.TotalSeconds);
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;

        if (hours >= 1)
        {
            return (new[]
            {
                new CardPart(hours.ToString("D2")),
                new CardPart(minutes.ToString("D2")),
                new CardPart(seconds.ToString("D2"))
            }, true);
        }

        return (new[]
        {
            new CardPart(minutes.ToString("D2")),
            new CardPart(seconds.ToString("D2"))
        }, false);
    }
}
