using System;
using System.IO;
using System.Text.Json;

namespace TimezoneTrayClock;

public class Settings
{
    public double? WindowLeft { get; set; } = null;
    public double? WindowTop { get; set; } = null;
    public bool IsClickThroughEnabled { get; set; } = false;
    public bool IsAutoTheme { get; set; } = true;
    public bool IsDarkMode { get; set; } = true;
    public bool IsAutoStartEnabled { get; set; } = false;
    public double Scale { get; set; } = 1.0;

    private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TimezoneTrayClock");
    private static readonly string SettingsFile = Path.Combine(AppDataFolder, "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
        }
        catch { }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppDataFolder);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            var tempFile = SettingsFile + ".tmp";
            File.WriteAllText(tempFile, json);
            File.Move(tempFile, SettingsFile, overwrite: true);
        }
        catch { }
    }
}
