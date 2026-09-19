namespace Countdown.Core.Settings;

/// <summary>Loads/saves the small local settings file.</summary>
public interface ISettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}
