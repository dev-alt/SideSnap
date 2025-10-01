using System.IO;
using SideSnap.Models;

namespace SideSnap.Utils;

public static class TodoImporter
{
    public static List<TodoItem> ImportFromMarkdown(string filePath)
    {
        var todos = new List<TodoItem>();

        if (!File.Exists(filePath))
            return todos;

        var lines = File.ReadAllLines(filePath);
        string currentCategory = "General";

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Check if it's a category header (starts with ##)
            if (trimmed.StartsWith("##"))
            {
                currentCategory = trimmed.TrimStart('#').Trim();
                continue;
            }

            // Check if it's a todo item (starts with [])
            if (trimmed.StartsWith("[]"))
            {
                var description = trimmed[2..].Trim();
                if (!string.IsNullOrWhiteSpace(description))
                {
                    todos.Add(new TodoItem
                    {
                        Category = currentCategory,
                        Description = description,
                        IsCompleted = false,
                        Priority = 2, // Medium priority by default
                        CreatedDate = DateTime.Now
                    });
                }
            }
        }

        return todos;
    }
}
