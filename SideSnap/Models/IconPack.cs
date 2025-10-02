namespace SideSnap.Models;

public class IconPack
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Icons { get; set; } = new();
}

public static class IconPacks
{
    public static Dictionary<string, IconPack> AvailablePacks { get; } = new()
    {
        ["Default"] = new IconPack
        {
            Name = "Default",
            Icons = new Dictionary<string, string>
            {
                ["Folder"] = "📁",
                ["PowerShell"] = "⚡",
                ["Executable"] = "⚙️",
                ["ShellScript"] = "🐚",
                ["Project"] = "📦",
                ["Layout"] = "🪟",
                ["Rule"] = "📋",
                ["Settings"] = "⚙",
                ["Add"] = "➕",
                ["Delete"] = "🗑️",
                ["Edit"] = "✏️",
                ["Capture"] = "📸"
            }
        },
        ["Minimal"] = new IconPack
        {
            Name = "Minimal",
            Icons = new Dictionary<string, string>
            {
                ["Folder"] = "▢",
                ["PowerShell"] = "›",
                ["Executable"] = "•",
                ["ShellScript"] = "‣",
                ["Project"] = "◆",
                ["Layout"] = "▣",
                ["Rule"] = "▤",
                ["Settings"] = "⚙",
                ["Add"] = "+",
                ["Delete"] = "×",
                ["Edit"] = "✎",
                ["Capture"] = "◉"
            }
        },
        ["Colorful"] = new IconPack
        {
            Name = "Colorful",
            Icons = new Dictionary<string, string>
            {
                ["Folder"] = "🗂️",
                ["PowerShell"] = "💻",
                ["Executable"] = "🔧",
                ["ShellScript"] = "🖥️",
                ["Project"] = "🎯",
                ["Layout"] = "🖼️",
                ["Rule"] = "📝",
                ["Settings"] = "⚙️",
                ["Add"] = "➕",
                ["Delete"] = "❌",
                ["Edit"] = "✏️",
                ["Capture"] = "📷"
            }
        },
        ["Professional"] = new IconPack
        {
            Name = "Professional",
            Icons = new Dictionary<string, string>
            {
                ["Folder"] = "⬚",
                ["PowerShell"] = "▶",
                ["Executable"] = "⬢",
                ["ShellScript"] = "⬡",
                ["Project"] = "◼",
                ["Layout"] = "▦",
                ["Rule"] = "▧",
                ["Settings"] = "⚙",
                ["Add"] = "＋",
                ["Delete"] = "－",
                ["Edit"] = "✎",
                ["Capture"] = "⊙"
            }
        }
    };

    public static string GetIcon(string packName, string iconKey, string fallback = "")
    {
        if (AvailablePacks.TryGetValue(packName, out var pack))
        {
            if (pack.Icons.TryGetValue(iconKey, out var icon))
            {
                return icon;
            }
        }

        // Try default pack
        if (packName != "Default" && AvailablePacks.TryGetValue("Default", out var defaultPack))
        {
            if (defaultPack.Icons.TryGetValue(iconKey, out var icon))
            {
                return icon;
            }
        }

        return fallback;
    }
}
