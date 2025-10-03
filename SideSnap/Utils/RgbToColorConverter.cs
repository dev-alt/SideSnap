using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SideSnap.Utils;

public class RgbToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string rgb)
        {
            try
            {
                var parts = rgb.Split(',');
                if (parts.Length == 3 &&
                    byte.TryParse(parts[0].Trim(), out byte r) &&
                    byte.TryParse(parts[1].Trim(), out byte g) &&
                    byte.TryParse(parts[2].Trim(), out byte b))
                {
                    return System.Windows.Media.Color.FromRgb(r, g, b);
                }
            }
            catch
            {
                // Return default color on error
            }
        }

        return System.Windows.Media.Color.FromRgb(99, 102, 241); // Default indigo
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Windows.Media.Color color)
        {
            return $"{color.R},{color.G},{color.B}";
        }
        return "99,102,241";
    }
}
