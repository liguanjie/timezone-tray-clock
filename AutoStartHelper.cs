using System;
using System.Reflection;
using Microsoft.Win32;

namespace TimezoneTrayClock;

public static class AutoStartHelper
{
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "TimezoneTrayClock";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
        var value = key?.GetValue(AppName) as string;
        if (string.IsNullOrEmpty(value)) return false;
        
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return false;
        
        return string.Equals(value, $"\"{exePath}\"", StringComparison.OrdinalIgnoreCase);
    }

    public static void SetAutoStart(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
        if (key == null) return;

        if (enable)
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath)) return;
            
            key.SetValue(AppName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}
