using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using SideSnap.Models;

namespace SideSnap.Services;

public class CommandExecutorService : ICommandExecutorService
{
    private readonly string _commandsPath;
    private readonly string[] _dangerousPatterns =
    [
        @"rm\s+-rf",
        @"Remove-Item.*-Recurse",
        @"Format-Volume",
        @"del\s+/[sS]",
        @"cipher\s+/w"
    ];

    public CommandExecutorService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "SideSnap");
        Directory.CreateDirectory(appFolder);
        _commandsPath = Path.Combine(appFolder, "commands.json");
    }

    public List<PowerShellCommand> GetCommands()
    {
        if (!File.Exists(_commandsPath))
        {
            return GetDefaultCommands();
        }

        try
        {
            var json = File.ReadAllText(_commandsPath);
            return JsonSerializer.Deserialize<List<PowerShellCommand>>(json) ?? GetDefaultCommands();
        }
        catch
        {
            return GetDefaultCommands();
        }
    }

    public void SaveCommands(IEnumerable<PowerShellCommand> commands)
    {
        var json = JsonSerializer.Serialize(commands, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_commandsPath, json);
    }

    public async Task ExecuteAsync(PowerShellCommand command)
    {
        // Validate command for dangerous patterns
        if (!ValidateCommand(command.Command))
        {
            throw new InvalidOperationException("Command contains potentially dangerous operations and cannot be executed.");
        }

        await Task.Run(() =>
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"{command.Command}\"",
                UseShellExecute = false,
                CreateNoWindow = command.RunHidden,
                RedirectStandardOutput = command.RunHidden,
                RedirectStandardError = command.RunHidden
            };

            if (command.RequiresElevation)
            {
                startInfo.Verb = "runas";
                startInfo.UseShellExecute = true;
            }

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
        });
    }

    private bool ValidateCommand(string command)
    {
        foreach (var pattern in _dangerousPatterns)
        {
            if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase))
            {
                return false;
            }
        }
        return true;
    }

    private List<PowerShellCommand> GetDefaultCommands()
    {
        return
        [
            new() { Name = "Open WSL", Command = "wsl", RunHidden = false },
            new() { Name = "System Info", Command = "systeminfo", RunHidden = false },
            new()
            {
                Name = "Network Status", Command = "Get-NetAdapter | Select-Object Name, Status, LinkSpeed",
                RunHidden = false
            }
        ];
    }
}