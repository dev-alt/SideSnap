using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SideSnap.Models;
using SideSnap.Services;
using SideSnap.Views;

namespace SideSnap.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ICommandExecutorService _commandExecutor;
    private readonly IShortcutService _shortcutService;
    private readonly IProjectService _projectService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private double _sidebarWidth = 250;

    [ObservableProperty]
    private ObservableCollection<FolderShortcut> _shortcuts = [];

    [ObservableProperty]
    private ObservableCollection<PowerShellCommand> _commands = [];

    [ObservableProperty]
    private ObservableCollection<Project> _projects = [];

    [ObservableProperty]
    private Project? _hoveredProject;

    public MainViewModel(
        ISettingsService settingsService,
        ICommandExecutorService commandExecutor,
        IShortcutService shortcutService,
        IProjectService projectService,
        ILogger<MainViewModel> logger,
        IServiceProvider serviceProvider)
    {
        _settingsService = settingsService;
        _commandExecutor = commandExecutor;
        _shortcutService = shortcutService;
        _projectService = projectService;
        _logger = logger;
        _serviceProvider = serviceProvider;

        LoadSettings();
        LoadShortcuts();
        LoadCommands();
        LoadProjects();
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsExpanded = !IsExpanded;
    }

    [RelayCommand]
    private async Task OpenShortcut(FolderShortcut shortcut)
    {
        await _shortcutService.OpenFolderAsync(shortcut.Path);
    }

    [RelayCommand]
    private async Task ExecuteCommand(PowerShellCommand command)
    {
        await _commandExecutor.ExecuteAsync(command);
    }

    [RelayCommand]
    private void AddShortcut()
    {
        var dialog = _serviceProvider.GetRequiredService<AddShortcutDialog>();
        dialog.Title = "Add Shortcut";
        dialog.HeaderText = "Add New Shortcut";
        dialog.PrimaryButtonText = "Add";
        if (dialog.ShowDialog() == true)
        {
            var shortcut = new FolderShortcut
            {
                Name = dialog.ShortcutName,
                Path = dialog.ShortcutPath,
                IconPath = dialog.CustomIconPath,
                Order = Shortcuts.Count,
                ShowLabel = dialog.ShowLabel
            };

            Shortcuts.Add(shortcut);
            SaveShortcuts();
            _logger.LogInformation("Added shortcut via dialog: {Name} -> {Path}", shortcut.Name, shortcut.Path);
        }
    }

    [RelayCommand]
    private void RemoveShortcut(FolderShortcut shortcut)
    {
        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete the shortcut '{shortcut.Name}'?",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            Shortcuts.Remove(shortcut);
            SaveShortcuts();
            _logger.LogInformation("Removed shortcut: {Name}", shortcut.Name);
        }
    }

    [RelayCommand]
    private void EditShortcut(FolderShortcut? shortcut)
    {
        if (shortcut == null) return;

        var dialog = _serviceProvider.GetRequiredService<AddShortcutDialog>();
        // Pre-populate with existing values and adjust UI text
        dialog.Title = "Edit Shortcut";
        dialog.HeaderText = "Edit Shortcut";
        dialog.PrimaryButtonText = "Save";
        dialog.Loaded += (_, _) =>
        {
            var nameBox = dialog.FindName("NameTextBox") as System.Windows.Controls.TextBox;
            var pathBox = dialog.FindName("PathTextBox") as System.Windows.Controls.TextBox;
            var iconPathBox = dialog.FindName("IconPathTextBox") as System.Windows.Controls.TextBox;
            var showLabelCheck = dialog.FindName("ShowLabelCheckBox") as System.Windows.Controls.CheckBox;
            if (nameBox != null) nameBox.Text = shortcut.Name;
            if (pathBox != null) pathBox.Text = shortcut.Path;
            if (iconPathBox != null) iconPathBox.Text = shortcut.IconPath;
            if (showLabelCheck != null) showLabelCheck.IsChecked = shortcut.ShowLabel;
        };

        if (dialog.ShowDialog() == true)
        {
            shortcut.Name = dialog.ShortcutName;
            shortcut.Path = dialog.ShortcutPath;
            shortcut.IconPath = dialog.CustomIconPath;
            shortcut.ShowLabel = dialog.ShowLabel;
            SaveShortcuts();
            // Force UI refresh
            var index = Shortcuts.IndexOf(shortcut);
            Shortcuts.RemoveAt(index);
            Shortcuts.Insert(index, shortcut);
            _logger.LogInformation("Edited shortcut: {Name}", shortcut.Name);
        }
    }

    [RelayCommand]
    private void MoveShortcutUp(FolderShortcut? shortcut)
    {
        if (shortcut == null) return;
        var index = Shortcuts.IndexOf(shortcut);
        if (index > 0)
        {
            Shortcuts.Move(index, index - 1);
            ReorderShortcuts();
            SaveShortcuts();
            _logger.LogDebug("Moved shortcut up: {Name}", shortcut.Name);
        }
    }

    [RelayCommand]
    private void MoveShortcutDown(FolderShortcut? shortcut)
    {
        if (shortcut == null) return;
        var index = Shortcuts.IndexOf(shortcut);
        if (index < Shortcuts.Count - 1)
        {
            Shortcuts.Move(index, index + 1);
            ReorderShortcuts();
            SaveShortcuts();
            _logger.LogDebug("Moved shortcut down: {Name}", shortcut.Name);
        }
    }

    [RelayCommand]
    private void RemoveCommand(PowerShellCommand command)
    {
        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete the command '{command.Name}'?",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            Commands.Remove(command);
            _commandExecutor.SaveCommands(Commands);
            _logger.LogInformation("Removed command: {Name}", command.Name);
        }
    }

    [RelayCommand]
    private void AddCommand()
    {
        var dialog = _serviceProvider.GetRequiredService<AddCommandDialog>();
        if (dialog.ShowDialog() == true)
        {
            var command = new PowerShellCommand
            {
                Name = dialog.CommandName,
                Command = dialog.CommandText,
                CustomIconPath = dialog.CustomIconPath,
                RunHidden = dialog.RunHidden,
                RequiresElevation = dialog.RequiresElevation,
                IsFavorite = dialog.IsFavorite,
                ShowLabel = dialog.ShowLabel,
                ScriptType = ScriptType.PowerShell
            };

            Commands.Add(command);
            _commandExecutor.SaveCommands(Commands);
            _logger.LogInformation("Added command via dialog: {Name}", command.Name);
        }
    }

    [RelayCommand]
    private void EditCommand(PowerShellCommand? command)
    {
        if (command == null) return;

        var dialog = _serviceProvider.GetRequiredService<AddCommandDialog>();
        dialog.Title = "Edit PowerShell Command";
        dialog.Loaded += (_, _) =>
        {
            var nameBox = dialog.FindName("NameTextBox") as System.Windows.Controls.TextBox;
            var commandBox = dialog.FindName("CommandTextBox") as System.Windows.Controls.TextBox;
            var iconPathBox = dialog.FindName("IconPathTextBox") as System.Windows.Controls.TextBox;
            var runHiddenCheck = dialog.FindName("RunHiddenCheckBox") as System.Windows.Controls.CheckBox;
            var elevationCheck = dialog.FindName("RequiresElevationCheckBox") as System.Windows.Controls.CheckBox;
            var favoriteCheck = dialog.FindName("IsFavoriteCheckBox") as System.Windows.Controls.CheckBox;
            var showLabelCheck = dialog.FindName("ShowLabelCheckBox") as System.Windows.Controls.CheckBox;

            if (nameBox != null) nameBox.Text = command.Name;
            if (commandBox != null) commandBox.Text = command.Command;
            if (iconPathBox != null) iconPathBox.Text = command.CustomIconPath;
            if (runHiddenCheck != null) runHiddenCheck.IsChecked = command.RunHidden;
            if (elevationCheck != null) elevationCheck.IsChecked = command.RequiresElevation;
            if (favoriteCheck != null) favoriteCheck.IsChecked = command.IsFavorite;
            if (showLabelCheck != null) showLabelCheck.IsChecked = command.ShowLabel;
        };

        if (dialog.ShowDialog() == true)
        {
            command.Name = dialog.CommandName;
            command.Command = dialog.CommandText;
            command.CustomIconPath = dialog.CustomIconPath;
            command.RunHidden = dialog.RunHidden;
            command.RequiresElevation = dialog.RequiresElevation;
            command.IsFavorite = dialog.IsFavorite;
            command.ShowLabel = dialog.ShowLabel;

            _commandExecutor.SaveCommands(Commands);
            // Force UI refresh
            var index = Commands.IndexOf(command);
            Commands.RemoveAt(index);
            Commands.Insert(index, command);
            _logger.LogInformation("Edited command: {Name}", command.Name);
        }
    }

    [RelayCommand]
    private void MoveCommandUp(PowerShellCommand? command)
    {
        if (command == null) return;
        var index = Commands.IndexOf(command);
        if (index > 0)
        {
            Commands.Move(index, index - 1);
            _commandExecutor.SaveCommands(Commands);
            _logger.LogDebug("Moved command up: {Name}", command.Name);
        }
    }

    [RelayCommand]
    private void MoveCommandDown(PowerShellCommand? command)
    {
        if (command == null) return;
        var index = Commands.IndexOf(command);
        if (index < Commands.Count - 1)
        {
            Commands.Move(index, index + 1);
            _commandExecutor.SaveCommands(Commands);
            _logger.LogDebug("Moved command down: {Name}", command.Name);
        }
    }

    [RelayCommand]
    private void OpenTodoTracker()
    {
        var todoWindow = _serviceProvider.GetRequiredService<TodoWindow>();
        todoWindow.Show();
        _logger.LogInformation("Opened todo tracker window");
    }

    private void ReorderShortcuts()
    {
        for (int i = 0; i < Shortcuts.Count; i++)
        {
            Shortcuts[i].Order = i;
        }
    }

    private void LoadSettings()
    {
        var settings = _settingsService.LoadSettings();
        SidebarWidth = settings.SidebarWidth;
    }

    private void LoadShortcuts()
    {
        var shortcuts = _shortcutService.GetShortcuts();
        Shortcuts = new ObservableCollection<FolderShortcut>(shortcuts);

        // Listen to collection changes to auto-save when reordering
        Shortcuts.CollectionChanged += (_, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move)
            {
                ReorderShortcuts();
                SaveShortcuts();
                _logger.LogDebug("Shortcuts reordered via drag-drop");
            }
        };
    }

    private void LoadCommands()
    {
        var commands = _commandExecutor.GetCommands();
        Commands = new ObservableCollection<PowerShellCommand>(commands);

        // Listen to collection changes to auto-save when reordering
        Commands.CollectionChanged += (_, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move)
            {
                _commandExecutor.SaveCommands(Commands);
                _logger.LogDebug("Commands reordered via drag-drop");
            }
        };
    }

    private void SaveShortcuts()
    {
        _shortcutService.SaveShortcuts(Shortcuts);
    }

    public void AddDroppedFolder(string path)
    {
        var name = Path.GetFileName(path);
        var shortcut = new FolderShortcut
        {
            Name = name,
            Path = path,
            Order = Shortcuts.Count
        };

        Shortcuts.Add(shortcut);
        SaveShortcuts();
        _logger.LogInformation("Added folder shortcut: {Name} -> {Path}", name, path);
    }

    public void AddDroppedExecutable(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var command = new PowerShellCommand
        {
            Name = name,
            Command = $"Start-Process '{path}'",
            RunHidden = false,
            ScriptType = ScriptType.Executable
        };

        Commands.Add(command);
        _commandExecutor.SaveCommands(Commands);
        _logger.LogInformation("Added executable command: {Name} -> {Path}", name, path);
    }

    public void AddDroppedShortcut(string path)
    {
        // For .lnk files, treat as folder shortcut
        var name = Path.GetFileNameWithoutExtension(path);
        var shortcut = new FolderShortcut
        {
            Name = name,
            Path = path,
            Order = Shortcuts.Count
        };

        Shortcuts.Add(shortcut);
        SaveShortcuts();
        _logger.LogInformation("Added shortcut: {Name} -> {Path}", name, path);
    }

    public void AddDroppedShellScript(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var settings = _settingsService.LoadSettings();

        // Convert Windows path to WSL path (e.g., C:\Users\... to /mnt/c/Users/...)
        var wslPath = ConvertToWslPath(path);

        var command = new PowerShellCommand
        {
            Name = name,
            Command = $"wsl bash \"{wslPath}\"",
            RunHidden = false,
            ShowLabel = settings.ShowLabelByDefault,
            ScriptType = ScriptType.Bash
        };

        Commands.Add(command);
        _commandExecutor.SaveCommands(Commands);
        _logger.LogInformation("Added shell script command: {Name} -> {Path}", name, path);
    }

    public void AddDroppedPowerShellScript(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var settings = _settingsService.LoadSettings();

        var command = new PowerShellCommand
        {
            Name = name,
            Command = $"& '{path}'",
            RunHidden = false,
            ShowLabel = settings.ShowLabelByDefault,
            ScriptType = ScriptType.PowerShell
        };

        Commands.Add(command);
        _commandExecutor.SaveCommands(Commands);
        _logger.LogInformation("Added PowerShell script command: {Name} -> {Path}", name, path);
    }

    private static string ConvertToWslPath(string windowsPath)
    {
        // Convert Windows path to WSL path format

        // Handle WSL UNC paths: \\wsl.localhost\Ubuntu-22.04\path -> /path
        // or \\wsl$\Ubuntu-22.04\path -> /path
        if (windowsPath.StartsWith(@"\\wsl.localhost\", StringComparison.OrdinalIgnoreCase) ||
            windowsPath.StartsWith(@"\\wsl$\", StringComparison.OrdinalIgnoreCase))
        {
            var parts = windowsPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                // Skip "wsl.localhost" (or "wsl$") and distribution name, take the rest
                var pathPart = string.Join("/", parts.Skip(2));
                return "/" + pathPart;
            }
        }

        // Handle regular Windows paths: C:\Users\... -> /mnt/c/Users/...
        if (windowsPath.Length >= 2 && windowsPath[1] == ':')
        {
            var drive = char.ToLower(windowsPath[0]);
            var pathPart = windowsPath.Substring(2).Replace('\\', '/');
            return $"/mnt/{drive}{pathPart}";
        }

        return windowsPath.Replace('\\', '/');
    }

    // Project Management
    private void LoadProjects()
    {
        var projects = _projectService.GetProjects();
        Projects = new ObservableCollection<Project>(projects);
    }

    private void SaveProjects()
    {
        _projectService.SaveProjects(Projects);
    }

    [RelayCommand]
    private void AddProject()
    {
        var dialog = _serviceProvider.GetRequiredService<AddProjectDialog>();
        dialog.Title = "Add Project";
        if (dialog.ShowDialog() == true)
        {
            var project = new Project
            {
                Name = dialog.ProjectName,
                IconPath = dialog.CustomIconPath,
                Order = Projects.Count
            };

            foreach (var item in dialog.Items)
            {
                project.Items.Add(item);
            }

            Projects.Add(project);
            SaveProjects();
            _logger.LogInformation("Added project: {Name} with {Count} items", project.Name, project.Items.Count);
        }
    }

    [RelayCommand]
    private void EditProject(Project? project)
    {
        if (project == null) return;

        var dialog = _serviceProvider.GetRequiredService<AddProjectDialog>();
        dialog.Title = "Edit Project";
        dialog.SetProject(project);

        if (dialog.ShowDialog() == true)
        {
            project.Name = dialog.ProjectName;
            project.IconPath = dialog.CustomIconPath;
            project.Items.Clear();

            foreach (var item in dialog.Items)
            {
                project.Items.Add(item);
            }

            SaveProjects();
            // Force UI refresh
            var index = Projects.IndexOf(project);
            Projects.RemoveAt(index);
            Projects.Insert(index, project);
            _logger.LogInformation("Edited project: {Name} with {Count} items", project.Name, project.Items.Count);
        }
    }

    [RelayCommand]
    private void RemoveProject(Project? project)
    {
        if (project == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete the project '{project.Name}' and all its items?",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            Projects.Remove(project);
            SaveProjects();
            _logger.LogInformation("Removed project: {Name}", project.Name);
        }
    }

    [RelayCommand]
    private void ExecuteProjectItem(ProjectItem? item)
    {
        if (item == null) return;

        switch (item.Type)
        {
            case ProjectItemType.Folder:
                _shortcutService.OpenFolderAsync(item.Path);
                break;
            case ProjectItemType.Script:
                var cmd = new PowerShellCommand
                {
                    Name = item.Name,
                    Command = item.Command,
                    RunHidden = false
                };
                _commandExecutor.ExecuteAsync(cmd);
                break;
            case ProjectItemType.Command:
                var command = new PowerShellCommand
                {
                    Name = item.Name,
                    Command = item.Command,
                    RunHidden = false
                };
                _commandExecutor.ExecuteAsync(command);
                break;
        }
    }

    [RelayCommand]
    private void ShowProjectDropdown(Project? project)
    {
        HoveredProject = project;
    }

    [RelayCommand]
    private void HideProjectDropdown()
    {
        HoveredProject = null;
    }
}