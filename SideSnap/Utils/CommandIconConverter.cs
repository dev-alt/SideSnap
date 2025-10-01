using System.Globalization;
using System.Windows.Data;
using SideSnap.Models;
using SideSnap.Services;

namespace SideSnap.Utils;

public class CommandIconConverter : IMultiValueConverter
{
    private static IIconService? _iconService;

    public static void Initialize(IIconService iconService)
    {
        _iconService = iconService;
    }

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // values[0] = CustomIconPath
        // values[1] = ScriptType

        // If custom icon is specified, use icon service to load it
        if (values.Length > 0 && values[0] is string customIcon && !string.IsNullOrWhiteSpace(customIcon) && _iconService != null)
        {
            return _iconService.GetIcon(customIcon);
        }

        // Otherwise, use the ScriptTypeToIconConverter logic
        if (values.Length > 1 && values[1] is ScriptType scriptType)
        {
            return scriptType switch
            {
                ScriptType.PowerShell => "📘",
                ScriptType.Bash => "🐧",
                ScriptType.Executable => "⚙️",
                ScriptType.Other => "⚡",
                _ => "⚡"
            };
        }

        return "⚡";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
