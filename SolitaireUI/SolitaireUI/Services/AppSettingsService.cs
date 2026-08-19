using System;
using System.IO;
using System.Text.Json;

namespace SolitaireUI.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> to a JSON file in the user's application data folder,
/// so game options and the selected card back persist across runs.
/// </summary>
public static class AppSettingsService
{
    private static readonly string SettingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SolitaireUI");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
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
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Persisting settings is best-effort; ignore failures (e.g. read-only environment).
        }
    }
}
