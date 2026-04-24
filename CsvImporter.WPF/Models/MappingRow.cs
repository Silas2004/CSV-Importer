using CommunityToolkit.Mvvm.ComponentModel;
using CsvImporter.Core.Models;

namespace CsvImporter.WPF.Models;

public partial class MappingRow : ObservableObject
{
    public required DbColumn DbColumn { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTypeWarning))]
    private CsvColumn? _selectedCsvColumn;

    [ObservableProperty] private MappingStatus _status    = MappingStatus.Unmatched;
    [ObservableProperty] private MappingMethod _method    = MappingMethod.None;
    [ObservableProperty] private double        _score;
    [ObservableProperty] private bool          _isIgnored;

    public string RequiredLabel => DbColumn.IsNullable ? "Optional" : "Pflicht";
    public bool   IsRequired    => !DbColumn.IsNullable;

    public bool HasTypeWarning =>
        SelectedCsvColumn is not null &&
        !IsTypeCompatible(SelectedCsvColumn.InferredType, DbColumn.DataType);

    private static bool IsTypeCompatible(InferredDataType csv, string db)
    {
        var d = db.ToUpperInvariant();
        return csv switch
        {
            InferredDataType.Integer  => d.Contains("INT") || d.Contains("NUMBER")  || d.Contains("NUMERIC") || d.Contains("DECIMAL"),
            InferredDataType.Decimal  => d.Contains("DECIMAL") || d.Contains("FLOAT") || d.Contains("REAL")  || d.Contains("NUMBER")  || d.Contains("NUMERIC"),
            InferredDataType.DateTime => d.Contains("DATE") || d.Contains("TIME") || d.Contains("STAMP"),
            InferredDataType.Date     => d.Contains("DATE"),
            InferredDataType.Boolean  => d.Contains("BIT") || d.Contains("BOOL") || d.Contains("NUMBER"),
            _                         => true,
        };
    }
}
