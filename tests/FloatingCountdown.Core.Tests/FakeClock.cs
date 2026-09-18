using FloatingCountdown.Core;

namespace FloatingCountdown.Core.Tests;

/// <summary>Manually controllable clock for deterministic time-based tests.</summary>
public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; }

    public FakeClock(DateTimeOffset? start = null)
    {
        UtcNow = start ?? new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    public void Advance(TimeSpan delta) => UtcNow += delta;
}
