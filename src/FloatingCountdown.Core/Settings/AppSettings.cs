namespace FloatingCountdown.Core.Settings;

/// <summary>
/// Persisted user preferences. First version stores window placement only;
/// running countdown state is intentionally never persisted.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Window top-left in device-independent pixels; null on first run.</summary>
    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    /// <summary>Virtual-screen geometry captured at save time, used to validate restore.</summary>
    public double? VirtualScreenLeft { get; set; }

    public double? VirtualScreenTop { get; set; }

    public double? VirtualScreenWidth { get; set; }

    public double? VirtualScreenHeight { get; set; }
}
