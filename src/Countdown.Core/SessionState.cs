namespace Countdown.Core;

/// <summary>Lifecycle states of the single countdown session.</summary>
public enum SessionState
{
    Idle,
    Running,
    Paused,
    Finished
}
