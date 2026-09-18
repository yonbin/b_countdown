namespace FloatingCountdown.Core;

/// <summary>Lifecycle states of the single countdown session.</summary>
public enum SessionState
{
    Idle,
    Running,
    Paused,
    Finished
}

/// <summary>Whether the alert sound is currently expected to play.</summary>
public enum AlertState
{
    Silent,
    Sounding
}
