using Countdown.Core;

namespace Countdown.Core.Tests;

public class DurationParserTests
{
    // ---------- US1: basic parsing ----------

    [Theory]
    [InlineData("25", 25)]
    [InlineData("1", 1)]
    public void TryParse_BareNumber_IsMinutes(string input, int expectedMinutes)
    {
        Assert.True(DurationParser.TryParse(input, out var duration));
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), duration);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    public void TryParse_InvalidInput_Fails(string? input)
    {
        Assert.False(DurationParser.TryParse(input, out var duration));
        Assert.Equal(TimeSpan.Zero, duration);
    }

    // ---------- US3: suffixes & combos ----------

    [Theory]
    [InlineData("5m", 5, 0)]
    [InlineData("90s", 1, 30)]
    [InlineData("1m30s", 1, 30)]
    [InlineData("0m45s", 0, 45)]
    [InlineData("90M", 90, 0)] // case-insensitive
    public void TryParse_Suffixed_ParsesCorrectly(string input, int minutes, int seconds)
    {
        Assert.True(DurationParser.TryParse(input, out var duration));
        Assert.Equal(new TimeSpan(0, minutes, seconds), duration);
    }

    [Theory]
    [InlineData("0s")]
    [InlineData("0m")]
    [InlineData("m")]
    [InlineData("s")]
    [InlineData("5mm")]
    [InlineData("1h")]
    public void TryParse_InvalidSuffixes_Fail(string input)
    {
        Assert.False(DurationParser.TryParse(input, out _));
    }

    // ---------- Convergence T039: long durations are not capped at 99h ----------

    [Theory]
    [InlineData("100", 100)]            // 100 minutes is fine
    [InlineData("599940", 599940)]      // exactly 9999 hours in minutes
    public void TryParse_LongBareNumbers_AreMinutes(string input, int expectedMinutes)
    {
        Assert.True(DurationParser.TryParse(input, out var duration));
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), duration);
    }

    [Theory]
    [InlineData("599941")]              // one minute past the 9999-hour safety ceiling
    [InlineData("36000000s")]           // 10000 hours in seconds
    [InlineData("99999999999999999999s")] // absurd input must not overflow
    public void TryParse_BeyondSafetyCeiling_Fails(string input)
    {
        Assert.False(DurationParser.TryParse(input, out _));
    }
}
