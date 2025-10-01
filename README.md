# SideSnap

A lightweight Windows desktop sidebar application for quick folder access, PowerShell command execution, and window management.

## Building

```bash
dotnet restore
dotnet build
```

## Running

```bash
dotnet run --project SideSnap
```

## Project Structure

- **Models**: Data models (AppSettings, FolderShortcut, PowerShellCommand, WindowPosition)
- **ViewModels**: MVVM ViewModels using CommunityToolkit.Mvvm
- **Views**: WPF XAML views
- **Services**: Business logic services
  - SettingsService: JSON-based configuration persistence
  - ShortcutService: Folder shortcut management
  - CommandExecutorService: PowerShell command execution with validation
  - WindowManagerService: Window positioning (Win32 API integration pending)
  - TrayService: System tray integration

## Features Implemented (MVP Phase)

✅ WPF project with MVVM architecture
✅ Left-docked sidebar window
✅ Folder shortcut management with JSON storage
✅ PowerShell command execution with safety validation
✅ System tray integration
✅ Dependency injection with Microsoft.Extensions.DI

## Next Steps (Phase 2)

- Auto-hide sidebar functionality
- Drag-and-drop folder shortcut creation
- Command history and favorites
- Multi-monitor support
- Window snap presets