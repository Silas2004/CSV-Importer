using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CsvImporter.Core.Models;

namespace CsvImporter.WPF.Converters;

public class MappingStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is MappingStatus status ? status switch
        {
            MappingStatus.Matched   => new SolidColorBrush(Colors.Green),
            MappingStatus.Warned    => new SolidColorBrush(Colors.Orange),
            MappingStatus.Unmatched => new SolidColorBrush(Colors.Red),
            _ => new SolidColorBrush(Colors.Gray)
        } : new SolidColorBrush(Colors.Gray);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
