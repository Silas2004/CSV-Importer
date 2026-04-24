using System.Globalization;
using System.Windows.Data;

namespace CsvImporter.WPF.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter is string s)
            return Enum.Parse(targetType, s);
        return Binding.DoNothing;
    }
}
