using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace SideSnap.Services;

public class StartupService : IStartupService
{
    private readonly ILogger<StartupService> _logger;
    private const string AppName = "SideSnap";
    private const string StartupKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, false);
            var value = key?.GetValue(AppName);
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check startup status");
            return false;
        }
    }

    public bool EnableStartup()
    {
        try
        {
            var exePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");

            // Verify the exe exists
            if (!File.Exists(exePath))
            {
                _logger.LogError("Executable not found at: {Path}", exePath);
                return false;
            }

            using var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, true);
            if (key != null)
            {
                key.SetValue(AppName, $"\"{exePath}\"");
                _logger.LogInformation("Enabled startup: {Path}", exePath);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable startup");
            return false;
        }
    }

    public bool DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, true);
            if (key != null)
            {
                key.DeleteValue(AppName, false);
                _logger.LogInformation("Disabled startup");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable startup");
            return false;
        }
    }
}
