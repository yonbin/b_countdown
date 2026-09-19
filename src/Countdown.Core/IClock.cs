namespace Countdown.Core;

/// <summary>
/// Wall-clock time source. Abstracted so that countdown time math
/// (including sleep/resume scenarios) can be tested deterministically.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>Production clock backed by the system wall clock.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
