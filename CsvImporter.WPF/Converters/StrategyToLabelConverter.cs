using System.Globalization;
using System.Windows.Data;
using CsvImporter.Core.Models;

namespace CsvImporter.WPF.Converters;

public class StrategyToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is ImportStrategy s ? s switch
        {
            ImportStrategy.WholeFile  => "Whole File",
            ImportStrategy.Sequential => "Sequential",
            ImportStrategy.Auto       => "Auto",
            _                         => s.ToString()
        } : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
