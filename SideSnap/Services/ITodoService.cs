using SideSnap.Models;

namespace SideSnap.Services;

public interface ITodoService
{
    IEnumerable<TodoItem> GetTodos();
    void SaveTodos(IEnumerable<TodoItem> todos);
    void AddTodo(TodoItem todo);
    void RemoveTodo(TodoItem todo);
    void ToggleComplete(TodoItem todo);
}
