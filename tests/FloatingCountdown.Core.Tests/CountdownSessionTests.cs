using FloatingCountdown.Core;

namespace FloatingCountdown.Core.Tests;

public class CountdownSessionTests
{
    private static CountdownSession NewSession(FakeClock clock) => new(clock);

    // ---------- US1: start & remaining ----------

    [Fact]
    public void Start_RecordsDurationAndCountsDownAgainstWallClock()
    {
        var clock = new FakeClock();
        var sut = NewSession(clock);

        sut.Start(TimeSpan.FromMinutes(10));

        Assert.Equal(SessionState.Running, sut.State);
        Assert.Equal(TimeSpan.FromMinutes(10), sut.Remaining);

        clock.Advance(TimeSpan.FromSeconds(65));

        Assert.Equal(8, sut.Remaining!.Value.Minutes);
        Assert.Equal(55, sut.Remaining!.Value.Seconds);
    }

    [Fact]
    public void Start_RejectsSubSecondDuration()
    {
        var sut = NewSession(new FakeClock());

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Start(TimeSpan.Zero));
        Assert.Equal(SessionState.Idle, sut.State);
    }

    [Fact]
    public void Start_WhileRunningIsRejected()
    {
        var sut = NewSession(new FakeClock());
        sut.Start(TimeSpan.FromMinutes(5));

        Assert.Throws<InvalidOperationException>(() => sut.Start(TimeSpan.FromMinutes(1)));
    }

    // ---------- US2: finish (stays at 00:00, no overtime counting) ----------

    [Fact]
    public void Refresh_AtEndTime_TransitionsToFinishedAndSounds()
    {
        var clock = new FakeClock();
        var sut = NewSession(clock);
        sut.Start(TimeSpan.FromMinutes(1));

        clock.Advance(TimeSpan.FromMinutes(1));
        sut.Refresh();

        Assert.Equal(SessionState.Finished, sut.State);
        Assert.Equal(AlertState.Sounding, sut.AlertState);
        Assert.Equal(TimeSpan.Zero, sut.Remaining);
    }

    [Fact]
    public void Refresh_LongAfterEnd_StaysFinishedAtZero()
    {
        var clock = new FakeClock();
        var sut = NewSession(clock);
        sut.Start(TimeSpan.FromMinutes(5));

        clock.Advance(TimeSpan.FromMinutes(125)); // 2 hours past end
        sut.Refresh();
        sut.Refresh();
        sut.Refresh();

        Assert.Equal(SessionState.Finished, sut.State);
        Assert.Equal(TimeSpan.Zero, sut.Remaining);
        Assert.Equal(AlertState.Sounding, sut.AlertState);
    }

    [Fact]
    public void Acknowledge_SilencesAlertButKeepsFinishedZeroState()
    {
        var clock = new FakeClock();
        var sut = NewSession(clock);
        sut.Start(TimeSpan.FromMinutes(1));
        clock.Advance(TimeSpan.FromMinutes(2));
        sut.Refresh();

        sut.Acknowledge();

        Assert.Equal(AlertState.Silent, sut.AlertState);
        Assert.Equal(SessionState.Finished, sut.State);
        Assert.Equal(TimeSpan.Zero, sut.Remaining);
    }

    [Fact]
    public void Reset_FromAnyState_GoesIdleSilentAndClearsTimingFields()
    {
        var clock = new FakeClock();
        var sut = NewSession(clock);
        sut.Start(TimeSpan.FromMinutes(1));
        clock.Advance(TimeSpan.FromMinutes(2));
        sut.Refresh();
        Assert.Equal(AlertState.Sounding, sut.AlertState);

        sut.Reset();

        Assert.Equal(SessionState.Idle, sut.State);
        Assert.Equal(AlertState.Silent, sut.AlertState);
        Assert.Null(sut.Remaining);
    }

    // ---------- US3: pause / resume / quick restart ----------

    [Fact]
    public void Pause_FreezesRemainingAndDoesNotCountWallTime()
    {
        var clock = new FakeClock();
        var sut = NewSession(clock);
        sut.Start(TimeSpan.FromMinutes(10));
        clock.Advance(TimeSpan.FromMinutes(4));

        sut.Pause();
        var frozen = sut.Remaining;
        clock.Advance(TimeSpan.FromMinutes(10)); // long real time passes while paused

        Assert.Equal(SessionState.Paused, sut.State);
        Assert.Equal(frozen, sut.Remaining);
    }

    [Fact]
    public void Resume_RebuildsEndTimeFromFrozenRemaining()
    {
        var clock = new FakeClock();
        var sut = NewSession(clock);
        sut.Start(TimeSpan.FromMinutes(10));
        clock.Advance(TimeSpan.FromMinutes(4)); // 6 min left
        sut.Pause();
        clock.Advance(TimeSpan.FromMinutes(10)); // ignored

        sut.Resume();
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.Equal(SessionState.Running, sut.State);
        Assert.Equal(5, sut.Remaining!.Value.Minutes);
    }

    [Fact]
    public void Pause_OnlyValidWhileRunning()
    {
        var sut = NewSession(new FakeClock());
        Assert.Throws<InvalidOperationException>(() => sut.Pause());
    }

    [Fact]
    public void Start_FromFinished_StopsAlertAndRestartsImmediately()
    {
        var clock = new FakeClock();
        var sut = NewSession(clock);
        sut.Start(TimeSpan.FromMinutes(1));
        clock.Advance(TimeSpan.FromMinutes(2));
        sut.Refresh();
        Assert.Equal(AlertState.Sounding, sut.AlertState);

        sut.Start(TimeSpan.FromMinutes(25)); // quick restart

        Assert.Equal(SessionState.Running, sut.State);
        Assert.Equal(AlertState.Silent, sut.AlertState);
        Assert.Equal(TimeSpan.FromMinutes(25), sut.Remaining);
    }
}
