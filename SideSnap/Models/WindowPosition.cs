namespace SideSnap.Models;

public class WindowPosition
{
    public string ProcessName { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Monitor { get; set; }
}