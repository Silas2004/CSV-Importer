using System.Globalization;
using System.Windows.Data;

namespace CsvImporter.WPF.Converters;

public class FileSizeConverter : IValueConverter
{
    private static readonly string[] Suffixes = { "B", "KB", "MB", "GB" };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long bytes) return "0 B";
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < Suffixes.Length - 1) { size /= 1024; i++; }
        return $"{size:F1} {Suffixes[i]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
