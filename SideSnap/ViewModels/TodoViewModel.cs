using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SideSnap.Models;
using SideSnap.Services;
using SideSnap.Utils;
using SideSnap.Views;

namespace SideSnap.ViewModels;

public partial class TodoViewModel : ViewModelBase
{
    private readonly ITodoService _todoService;
    private readonly ILogger<TodoViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableCollection<TodoItem> _todos = [];

    [ObservableProperty]
    private ObservableCollection<TodoItem> _filteredTodos = [];

    [ObservableProperty]
    private ObservableCollection<string> _categories = [];

    [ObservableProperty]
    private string? _selectedCategory = "All";

    [ObservableProperty]
    private bool _showAll = true;

    [ObservableProperty]
    private bool _showPending;

    [ObservableProperty]
    private bool _showCompleted;

    public TodoViewModel(
        ITodoService todoService,
        ILogger<TodoViewModel> logger,
        IServiceProvider serviceProvider)
    {
        _todoService = todoService;
        _logger = logger;
        _serviceProvider = serviceProvider;

        LoadTodos();
        PropertyChanged += OnFilterChanged;
    }

    private void OnFilterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShowAll) or nameof(ShowPending) or nameof(ShowCompleted) or nameof(SelectedCategory))
        {
            ApplyFilter();
        }
    }

    private void LoadTodos()
    {
        var todos = _todoService.GetTodos().ToList();
        Todos = new ObservableCollection<TodoItem>(todos);

        // Extract unique categories
        var categories = todos.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
        categories.Insert(0, "All");
        Categories = new ObservableCollection<string>(categories);

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = Todos.AsEnumerable();

        // Filter by completion status
        if (ShowPending)
            filtered = filtered.Where(t => !t.IsCompleted);
        else if (ShowCompleted)
            filtered = filtered.Where(t => t.IsCompleted);

        // Filter by category
        if (SelectedCategory is not null && SelectedCategory != "All")
            filtered = filtered.Where(t => t.Category == SelectedCategory);

        FilteredTodos = new ObservableCollection<TodoItem>(filtered);
    }

    [RelayCommand]
    private void AddTodo()
    {
        var dialog = _serviceProvider.GetRequiredService<AddTodoDialog>();
        if (dialog.ShowDialog() == true)
        {
            var todo = new TodoItem
            {
                Category = dialog.TodoCategory,
                Description = dialog.TodoDescription,
                Priority = dialog.TodoPriority,
                CreatedDate = DateTime.Now
            };

            Todos.Add(todo);
            _todoService.SaveTodos(Todos);

            // Update categories if new category
            if (!Categories.Contains(todo.Category))
            {
                var cats = Categories.ToList();
                cats.Add(todo.Category);
                Categories = new ObservableCollection<string>(cats.OrderBy(c => c == "All" ? "" : c));
            }

            ApplyFilter();
            _logger.LogInformation("Added todo: {Description}", todo.Description);
        }
    }

    [RelayCommand]
    private void RemoveTodo(TodoItem? todo)
    {
        if (todo is null) return;

        Todos.Remove(todo);
        _todoService.SaveTodos(Todos);
        ApplyFilter();
        _logger.LogInformation("Removed todo: {Description}", todo.Description);
    }

    [RelayCommand]
    private void ToggleTodo(TodoItem? todo)
    {
        if (todo is null) return;

        _todoService.ToggleComplete(todo);
        _todoService.SaveTodos(Todos);

        // Trigger UI refresh
        var index = Todos.IndexOf(todo);
        if (index >= 0)
        {
            Todos.RemoveAt(index);
            Todos.Insert(index, todo);
        }

        ApplyFilter();
        _logger.LogInformation("Toggled todo: {Description} -> {Status}", todo.Description, todo.IsCompleted ? "Completed" : "Pending");
    }

    [RelayCommand]
    private void ImportFromFile()
    {
        // Try to find TODO.md in the project root
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "docs", "TODO.md"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "docs", "TODO.md"),
            Path.Combine(Directory.GetCurrentDirectory(), "TODO.md")
        };

        string? todoFilePath = null;
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                todoFilePath = path;
                break;
            }
        }

        if (todoFilePath is null)
        {
            MessageBox.Show("Could not find TODO.md file. Please ensure it exists in the docs folder.",
                "Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var importedTodos = TodoImporter.ImportFromMarkdown(todoFilePath);

        if (importedTodos.Count == 0)
        {
            MessageBox.Show("No todos found in the file.",
                "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Add imported todos
        foreach (var todo in importedTodos)
        {
            Todos.Add(todo);
        }

        _todoService.SaveTodos(Todos);

        // Update categories
        var categories = Todos.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
        categories.Insert(0, "All");
        Categories = new ObservableCollection<string>(categories);

        ApplyFilter();

        MessageBox.Show($"Successfully imported {importedTodos.Count} todos from TODO.md",
            "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);

        _logger.LogInformation("Imported {Count} todos from {Path}", importedTodos.Count, todoFilePath);
    }
}
