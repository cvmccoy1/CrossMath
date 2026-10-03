using System.IO;
using CrossMath.App.Services;
using CrossMath.Core;

namespace CrossMath.App.Tests;

public sealed class JsonSettingsServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CrossMathTests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_dir, "sub", "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var saved = new AppSettings(Difficulty.Hard, new WindowPlacement(-1200.5, 40, 820, 900, IsMaximized: true));
        new JsonSettingsService(SettingsPath).Save(saved);

        Assert.Equal(saved, new JsonSettingsService(SettingsPath).Load());
        Assert.Contains("\"Hard\"", File.ReadAllText(SettingsPath)); // readable enum names
    }

    [Fact]
    public void Load_MissingFile_GivesDefaults()
    {
        Assert.Equal(new AppSettings(), new JsonSettingsService(SettingsPath).Load());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "Difficulty": "Impossible" }""")]
    [InlineData("""{ "Difficulty": 42 }""")]
    public void Load_BadFile_GivesDefaults(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, content);

        Assert.Equal(Difficulty.Easy, new JsonSettingsService(SettingsPath).Load().Difficulty);
    }
}
