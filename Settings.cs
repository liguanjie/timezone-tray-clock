using System;
using System.IO;
using System.Text.Json;

namespace TimezoneTrayClock;

public class Settings : ObservableObject
{
    private double? _windowLeft = null;
    public double? WindowLeft
    {
        get => _windowLeft;
        set => SetProperty(ref _windowLeft, value);
    }

    private double? _windowTop = null;
    public double? WindowTop
    {
        get => _windowTop;
        set => SetProperty(ref _windowTop, value);
    }

    private bool _isClickThroughEnabled = false;
    public bool IsClickThroughEnabled
    {
        get => _isClickThroughEnabled;
        set => SetProperty(ref _isClickThroughEnabled, value);
    }

    private bool _isAutoTheme = true;
    public bool IsAutoTheme
    {
        get => _isAutoTheme;
        set => SetProperty(ref _isAutoTheme, value);
    }

    private bool _isDarkMode = true;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => SetProperty(ref _isDarkMode, value);
    }

    private bool _isAutoStartEnabled = false;
    public bool IsAutoStartEnabled
    {
        get => _isAutoStartEnabled;
        set => SetProperty(ref _isAutoStartEnabled, value);
    }

    private double _scale = 1.0;
    public double Scale
    {
        get => _scale;
        set => SetProperty(ref _scale, value);
    }

    private string _backgroundColor = "#D6DEF1";
    public string BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}
