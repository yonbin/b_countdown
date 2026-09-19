using Microsoft.Win32;

namespace Countdown.App.Services;

/// <summary>Raises <see cref="Resumed"/> when the machine wakes from sleep.</summary>
public sealed class PowerWatcher : IDisposable
{
    public event EventHandler? Resumed;

    private bool _disposed;

    public PowerWatcher()
    {
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Resumed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _disposed = true;
    }
}
