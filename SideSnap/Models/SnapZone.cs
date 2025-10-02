namespace SideSnap.Models;

public enum SnapZone
{
    LeftHalf,
    RightHalf,
    TopHalf,
    BottomHalf,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center,
    Maximize
}

public class SnapZoneDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public SnapZone Zone { get; set; }
    public int Order { get; set; }
}
