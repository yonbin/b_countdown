using Countdown.Core;

namespace Countdown.Core.Tests;

/// <summary>
/// Sleep is simulated by jumping the FakeClock forward without ticks:
/// the session derives remaining from the absolute end time, so a wake-up
/// Refresh() must produce the same result it would at the wall clock.
/// </summary>
public class SleepResumeTests
{
    [Fact]
    public void Refresh_AfterSleepShorterThanRemaining_RecomputesCorrectly()
    {
        var clock = new FakeClock();
        var sut = new CountdownSession(clock);
        sut.Start(TimeSpan.FromMinutes(5));

        // Simulate sleeping for 1 minute (no ticks during sleep).
        clock.Advance(TimeSpan.FromMinutes(1));
        sut.Refresh();

        Assert.Equal(SessionState.Running, sut.State);
        Assert.Equal(4, sut.Remaining!.Value.Minutes);
        Assert.Equal(0, sut.Remaining!.Value.Seconds);
    }

    [Fact]
    public void Refresh_AfterSleepAcrossEndTime_FinishesAtZero()
    {
        var clock = new FakeClock();
        var sut = new CountdownSession(clock);
        sut.Start(TimeSpan.FromMinutes(1));

        // Lid closed for 3 minutes: wake up 2 minutes past the end.
        clock.Advance(TimeSpan.FromMinutes(3));
        sut.Refresh();

        Assert.Equal(SessionState.Finished, sut.State);
        Assert.Equal(TimeSpan.Zero, sut.Remaining);

        // No overtime counting: further refreshes keep the zero finished state.
        sut.Refresh();
        Assert.Equal(SessionState.Finished, sut.State);
        Assert.Equal(TimeSpan.Zero, sut.Remaining);
    }

    [Fact]
    public void Refresh_OnPausedSession_AfterWallTimeJumps_KeepsFrozenValue()
    {
        var clock = new FakeClock();
        var sut = new CountdownSession(clock);
        sut.Start(TimeSpan.FromMinutes(10));
        clock.Advance(TimeSpan.FromMinutes(2));
        sut.Pause();

        clock.Advance(TimeSpan.FromHours(2)); // paused through a long sleep
        sut.Refresh();

        Assert.Equal(SessionState.Paused, sut.State);
        Assert.Equal(8, sut.Remaining!.Value.Minutes);
    }
}
