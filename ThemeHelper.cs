using System;
using System.Windows;
using Microsoft.Win32;

namespace TimezoneTrayClock;

public static class ThemeHelper
{
    public static void ApplyTheme(bool isDark)
    {
        var app = Application.Current;
        var uri = new Uri(isDark ? "DarkTheme.xaml" : "LightTheme.xaml", UriKind.Relative);
        
        for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            var dict = app.Resources.MergedDictionaries[i];
            if (dict.Source != null && dict.Source.OriginalString.Contains("Theme.xaml"))
            {
                app.Resources.MergedDictionaries.RemoveAt(i);
            }
        }

        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
    }

    public static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int i && i == 1)
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to read system theme: {ex.Message}");
        }
        return true;
    }
}
