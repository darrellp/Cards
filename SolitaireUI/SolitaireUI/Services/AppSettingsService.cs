using System;
using System.IO;
using System.Text.Json;

namespace SolitaireUI.Services;

/// <summary>
/// Abstraction over the raw storage medium used to persist settings text
/// (a JSON blob). Desktop platforms use a file on disk; the browser platform
/// can plug in a different implementation (e.g. localStorage) since it has
/// no real filesystem.
/// </summary>
public interface ISettingsStore
{
    string? Read();
    void Write(string json);
}

/// <summary>
/// Default store used on platforms with a real filesystem. Persists to a
/// JSON file in the user's application data folder.
/// </summary>
public sealed class FileSettingsStore : ISettingsStore
{
    private static readonly string SettingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SolitaireUI");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    public string? Read()
    {
        return File.Exists(SettingsFilePath) ? File.ReadAllText(SettingsFilePath) : null;
    }

    public void Write(string json)
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsFilePath, json);
    }
}

/// <summary>
/// Loads and saves <see cref="AppSettings"/> via the current <see cref="Store"/>,
/// so game options and the selected card back persist across runs.
/// </summary>
public static class AppSettingsService
{
    /// <summary>
    /// The storage backend to use. Defaults to file-based storage. Platform
    /// entry points (e.g. the Browser project's Program.cs) can replace this
    /// before the app starts to use a different storage medium.
    /// </summary>
    public static ISettingsStore Store { get; set; } = new FileSettingsStore();

    public static AppSettings Load()
    {
        try
        {
            var json = Store.Read();
            if (json != null)
            {
                var settings = JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings);
                if (settings != null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // Fall through to defaults if the file is missing, unreadable, or malformed.
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
            Store.Write(json);
        }
        catch
        {
            // Persisting settings is best-effort; ignore failures (e.g. read-only environment).
        }
    }
}

