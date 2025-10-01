using System.Globalization;
using System.Windows.Data;
using SideSnap.Models;

namespace SideSnap.Utils;

public class ScriptTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ScriptType scriptType)
        {
            return scriptType switch
            {
                ScriptType.PowerShell => "📘",  // Blue book for PowerShell
                ScriptType.Bash => "🐧",        // Penguin for Bash/Linux
                ScriptType.Executable => "⚙️",  // Gear for executables
                ScriptType.Other => "⚡",       // Lightning bolt for other
                _ => "⚡"
            };
        }
        return "⚡";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
