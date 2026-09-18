using System.IO;
using System.Media;
using System.Reflection;

namespace FloatingCountdown.App.Services;

/// <summary>
/// Plays the embedded alert sound locally and looping. It deliberately bypasses
/// Windows toast/notification channels so focus-assist does not suppress it.
/// Missing/unavailable audio degrades silently.
/// </summary>
public sealed class AudioAlertService : IDisposable
{
    private readonly object _gate = new();
    private SoundPlayer? _player;
    private MemoryStream? _audioBuffer;
    private bool _disposed;

    public AudioAlertService()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = Array.Find(
                assembly.GetManifestResourceNames(),
                n => n.EndsWith("alert.wav", StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
            {
                return;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                return;
            }

            // SoundPlayer requires the backing stream to stay alive; keep it for the app lifetime.
            _audioBuffer = new MemoryStream();
            stream.CopyTo(_audioBuffer);
            _player = new SoundPlayer(_audioBuffer);
            _player.Load();
        }
        catch
        {
            _player = null;
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            try
            {
                _player?.PlayLooping();
            }
            catch
            {
                // No audio device / muted: visual state still signals completion.
            }
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            try
            {
                _player?.Stop();
            }
            catch
            {
                // Ignore.
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _player?.Dispose();
        _audioBuffer?.Dispose();
        _disposed = true;
    }
}
