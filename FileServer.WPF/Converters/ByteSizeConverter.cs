using System.Globalization;
using System.Windows.Data;

namespace FileServer.WPF.Converters;

public class ByteSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return bytes switch
            {
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{bytes / 1024.0:F2} KB",
                < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F2} MB",
                _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
            };
        }
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}