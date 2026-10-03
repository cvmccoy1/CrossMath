using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrossMath.Core;

namespace CrossMath.App.Services;

/// <summary>Stores settings as JSON, by default in %LOCALAPPDATA%\CrossMath\settings.json.</summary>
public sealed class JsonSettingsService(string path) : ISettingsService
{
    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrossMath", "settings.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(path)) return new AppSettings();
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Options) ?? new AppSettings();
            return Enum.IsDefined(settings.Difficulty) ? settings : settings with { Difficulty = Difficulty.Easy };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, Options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Not worth interrupting the game over; the next save will try again.
        }
    }
}
