namespace SideSnap.Models;

public class ClipboardItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public ClipboardItemType Type { get; set; } = ClipboardItemType.Text;
    public DateTime CopiedAt { get; set; } = DateTime.Now;
    public bool IsPinned { get; set; }
}

public enum ClipboardItemType
{
    Text,
    Image,
    File
}
