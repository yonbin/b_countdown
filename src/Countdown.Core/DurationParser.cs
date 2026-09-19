using System.Text.RegularExpressions;

namespace Countdown.Core;

/// <summary>
/// Parses user-entered durations:
/// bare digits are minutes ("25" = 25 min); optional m/s suffixes and
/// combinations are accepted ("5m", "90s", "1m30s"). Minimum valid value is 1 second.
/// </summary>
public static partial class DurationParser
{
    [GeneratedRegex(@"^\s*(?:(?<minutes>\d+)m)?(?:(?<seconds>\d+)s)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Suffixed();

    private static readonly Regex BareDigits = new(@"^\s*\d+\s*$", RegexOptions.CultureInvariant);

    // Spec places no functional maximum on duration (display polish above 99 hours
    // is not guaranteed); this is only an overflow-safety ceiling.
    private const long MaxHours = 9999;

    public static bool TryParse(string? text, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        long minutes;
        long seconds;

        if (BareDigits.IsMatch(trimmed))
        {
            if (!long.TryParse(trimmed, out minutes))
            {
                return false;
            }

            seconds = 0;
        }
        else
        {
            var match = Suffixed().Match(trimmed);
            if (!match.Success)
            {
                return false;
            }

            minutes = 0;
            seconds = 0;
            var minutesGroup = match.Groups["minutes"];
            var secondsGroup = match.Groups["seconds"];

            if (!minutesGroup.Success && !secondsGroup.Success)
            {
                return false; // "m" / "s" alone
            }

            if (minutesGroup.Success && !long.TryParse(minutesGroup.Value, out minutes))
            {
                return false;
            }

            if (secondsGroup.Success && !long.TryParse(secondsGroup.Value, out seconds))
            {
                return false;
            }
        }

        // Bound components before arithmetic so huge digit runs cannot overflow TimeSpan.
        if (minutes < 0 || seconds < 0 ||
            minutes > MaxHours * 60 || seconds > MaxHours * 3600)
        {
            return false;
        }

        var parsed = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);

        if (parsed < CountdownSession.MinimumDuration || parsed > TimeSpan.FromHours(MaxHours))
        {
            return false;
        }

        duration = parsed;
        return true;
    }
}
