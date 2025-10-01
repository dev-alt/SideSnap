using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SideSnap.Models;

namespace SideSnap.Services;

public class TodoService : ITodoService
{
    private readonly string _todoFilePath;
    private readonly ILogger<TodoService> _logger;

    public TodoService(ILogger<TodoService> logger)
    {
        _logger = logger;
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SideSnap");
        Directory.CreateDirectory(appDataPath);
        _todoFilePath = Path.Combine(appDataPath, "todos.json");
    }

    public IEnumerable<TodoItem> GetTodos()
    {
        if (!File.Exists(_todoFilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_todoFilePath);
            return JsonSerializer.Deserialize<List<TodoItem>>(json) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load todos from {Path}", _todoFilePath);
            return [];
        }
    }

    public void SaveTodos(IEnumerable<TodoItem> todos)
    {
        try
        {
            var json = JsonSerializer.Serialize(todos, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_todoFilePath, json);
            _logger.LogDebug("Saved todos to {Path}", _todoFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save todos to {Path}", _todoFilePath);
        }
    }

    public void AddTodo(TodoItem todo)
    {
        var todos = GetTodos().ToList();
        todos.Add(todo);
        SaveTodos(todos);
    }

    public void RemoveTodo(TodoItem todo)
    {
        var todos = GetTodos().ToList();
        todos.Remove(todo);
        SaveTodos(todos);
    }

    public void ToggleComplete(TodoItem todo)
    {
        todo.IsCompleted = !todo.IsCompleted;
        todo.CompletedDate = todo.IsCompleted ? DateTime.Now : null;
    }
}
