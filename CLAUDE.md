# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
SideSnap is a Windows desktop sidebar application built with WPF and C#. It provides:
- Quick access to favorite folders via a left-docked sidebar
- PowerShell command launcher with custom command support
- Window positioning and management for multi-monitor setups
- Auto-hide functionality with hover activation

## Technology Stack
- **UI Framework**: WPF (Windows Presentation Foundation) with C#
- **Architecture**: MVVM pattern
- **Window Management**: Win32 API (SetWindowPos, EnumWindows)
- **Command Execution**: System.Diagnostics.Process for PowerShell
- **Storage**: JSON files or SQLite for configurations and user preferences
- **Target Platform**: Windows desktop (.NET 8 or later recommended)

## Architecture

### Module Structure
The application is organized into the following core modules:

1. **Sidebar UI**: Handles rendering, animations, auto-hide behavior, and docking to screen edge
2. **Shortcut Manager**: Manages folder shortcuts including add/remove/reorder operations and drag-and-drop
3. **Command Executor**: Executes and manages PowerShell commands with history and favorites
4. **Window Manager**: Controls application window positioning, snap zones, and multi-monitor support
5. **Settings Manager**: Persists user preferences, themes, and configuration profiles
6. **Tray Integration**: System tray icon with context menu for quick access

### Key Design Principles
- Use MVVM to separate UI from business logic
- All PowerShell command execution must validate input to prevent malicious commands
- Support multi-monitor environments with proper DPI scaling
- Configuration stored as JSON files for easy backup and portability
- Implement auto-hide with smooth WPF animations for sidebar show/hide

## Development Phases

### Phase 1 (MVP):
- WPF project with left-docked sidebar
- Folder shortcut management (add/remove/reorder) with persistent storage
- Basic PowerShell command execution interface
- Manual window positioning for selected applications

### Phase 2:
- Auto-hide sidebar with mouse hover detection
- Drag-and-drop folder shortcut creation
- Command history and favorites
- Multi-monitor support and window snap presets

### Phase 3:
- Folder preview on hover
- Window position memory per application
- Light/dark theme support
- System tray integration with toggle functionality

## Security Requirements
- **Command Validation**: All PowerShell commands must be validated before execution
- **UAC Integration**: Prompt for elevation when required
- **Safe Defaults**: Limit execution to safe operations unless explicitly allowed by user
- **Secure Storage**: User preferences should support optional encryption

## Windows-Specific Considerations
- Use P/Invoke for Win32 API calls (SetWindowPos, EnumWindows, etc.)
- Handle multi-monitor DPI scaling properly
- Implement proper window message handling for auto-hide behavior
- Support Windows 10/11 design guidelines for modern appearance
- Register for startup with Windows via registry or startup folder