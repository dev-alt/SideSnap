using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SideSnap.Utils;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isEnabled)
        {
            return new SolidColorBrush(isEnabled ? Color.FromRgb(39, 174, 96) : Color.FromRgb(192, 57, 43));
        }
        return new SolidColorBrush(Color.FromRgb(192, 57, 43));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
