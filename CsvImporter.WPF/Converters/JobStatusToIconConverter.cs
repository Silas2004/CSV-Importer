using System.Globalization;
using System.Windows.Data;
using CsvImporter.Core.Models;

namespace CsvImporter.WPF.Converters;

public class JobStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is JobStatus s ? s switch
        {
            JobStatus.Pending => "⏳",
            JobStatus.Running => "▶",
            JobStatus.Done    => "✔",
            JobStatus.Failed  => "✖",
            JobStatus.Skipped => "⏭",
            _                 => "?"
        } : "?";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
