namespace CrossMath.App.Services;

/// <summary>Loads and saves <see cref="AppSettings"/>, abstracted so view models can be tested with a fake.</summary>
public interface ISettingsService
{
    /// <summary>The saved settings, or defaults if there are none or they can't be read.</summary>
    AppSettings Load();

    /// <summary>Saves the settings. Failures are ignored, since settings are only a convenience.</summary>
    void Save(AppSettings settings);
}
