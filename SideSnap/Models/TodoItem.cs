namespace SideSnap.Models;

public class TodoItem
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Priority { get; set; } // 1 = High, 2 = Medium, 3 = Low
    public DateTime? CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}
