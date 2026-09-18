namespace FloatingCountdown.Core;

/// <summary>
/// A single countdown. Remaining time is always derived from an absolute
/// end instant on the injected clock, so system sleep/resume cannot cause
/// drift: the ticking UI timer only triggers a re-read, it never counts.
/// At zero the session stays Finished (it never counts overtime upward).
/// </summary>
public sealed class CountdownSession
{
    public static readonly TimeSpan MinimumDuration = TimeSpan.FromSeconds(1);

    private readonly IClock _clock;
    private DateTimeOffset? _endTime;
    private TimeSpan? _remainingOnPause;

    public CountdownSession(IClock clock)
    {
        _clock = clock;
    }

    public SessionState State { get; private set; } = SessionState.Idle;

    public AlertState AlertState { get; private set; } = AlertState.Silent;

    public TimeSpan Duration { get; private set; }

    /// <summary>Remaining time while Running/Paused; null while Idle; Zero after finishing.</summary>
    public TimeSpan? Remaining => State switch
    {
        SessionState.Idle => null,
        SessionState.Running => _endTime is { } end
            ? (end - _clock.UtcNow) is var r && r > TimeSpan.Zero ? r : TimeSpan.Zero
            : TimeSpan.Zero,
        SessionState.Paused => _remainingOnPause,
        SessionState.Finished => TimeSpan.Zero,
        _ => null
    };

    /// <summary>
    /// Start a new countdown. Allowed from Idle and from Finished
    /// (quick restart silences any alert first). Running/Paused must reset first.
    /// </summary>
    public void Start(TimeSpan duration)
    {
        if (duration < MinimumDuration)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration,
                "Duration must be at least 1 second.");
        }

        if (State is SessionState.Running or SessionState.Paused)
        {
            throw new InvalidOperationException("Reset the current countdown before starting a new one.");
        }

        AlertState = AlertState.Silent;
        Duration = duration;
        _remainingOnPause = null;
        _endTime = _clock.UtcNow + duration;
        State = SessionState.Running;
    }

    public void Pause()
    {
        if (State != SessionState.Running)
        {
            throw new InvalidOperationException("Only a running countdown can be paused.");
        }

        _remainingOnPause = Remaining;
        _endTime = null;
        State = SessionState.Paused;
    }

    public void Resume()
    {
        if (State != SessionState.Paused)
        {
            throw new InvalidOperationException("Only a paused countdown can be resumed.");
        }

        _endTime = _clock.UtcNow + _remainingOnPause!.Value;
        _remainingOnPause = null;
        State = SessionState.Running;
    }

    public void Reset()
    {
        AlertState = AlertState.Silent;
        _endTime = null;
        _remainingOnPause = null;
        Duration = TimeSpan.Zero;
        State = SessionState.Idle;
    }

    /// <summary>Stop the alert while keeping the Finished visual state (00:00).</summary>
    public void Acknowledge()
    {
        if (State == SessionState.Finished)
        {
            AlertState = AlertState.Silent;
        }
    }

    /// <summary>
    /// Re-evaluate state against the wall clock. Called by the UI tick and
    /// immediately after a system resume event.
    /// </summary>
    public void Refresh()
    {
        if (State == SessionState.Running && _endTime is { } end && _clock.UtcNow >= end)
        {
            AlertState = AlertState.Sounding;
            State = SessionState.Finished;
        }
    }
}
