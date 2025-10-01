using System.Globalization;
using System.Windows.Data;
using SideSnap.Services;

namespace SideSnap.Utils;

public class CustomIconConverter : IMultiValueConverter
{
    private static IIconService? _iconService;

    public static void Initialize(IIconService iconService)
    {
        _iconService = iconService;
    }

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (_iconService == null)
            return Binding.DoNothing;

        // values[0] = CustomIconPath or IconPath
        // values[1] = Path (for shortcuts) or null (for commands)

        if (values.Length > 0 && values[0] is string customIcon && !string.IsNullOrWhiteSpace(customIcon))
        {
            // Use custom icon if specified
            return _iconService.GetIcon(customIcon);
        }

        if (values.Length > 1 && values[1] is string fallbackPath && !string.IsNullOrWhiteSpace(fallbackPath))
        {
            // Use default icon from path
            return _iconService.GetIcon(fallbackPath);
        }

        return Binding.DoNothing;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
