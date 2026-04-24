using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsvImporter.Core.Models;
using CsvImporter.Core.Services;
using CsvImporter.Mapping;
using CsvImporter.WPF.Models;

namespace CsvImporter.WPF.ViewModels;

public partial class MappingViewModel : ObservableObject, IWizardStep
{
    private readonly ImportContext      _context;
    private readonly AppSettingsService _settings;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanProceed))]
    private ObservableCollection<MappingRow> _mappingRows = new();

    [ObservableProperty] private List<CsvColumn> _csvColumns = new();

    public bool CanProceed => MappingRows.All(r =>
        r.IsIgnored ||
        r.SelectedCsvColumn is not null ||
        r.DbColumn.IsNullable);

    public MappingViewModel(ImportContext context, AppSettingsService settings)
    {
        _context  = context;
        _settings = settings;
    }

    public async Task EnterAsync()
    {
        CsvColumns = BuildCsvColumns();
        await RunAutoMatchAsync();
    }

    [RelayCommand]
    private async Task AutoMatchAsync() => await RunAutoMatchAsync();

    [RelayCommand]
    private void ResetMapping()
    {
        foreach (var r in MappingRows)
        {
            r.SelectedCsvColumn = null;
            r.Status    = MappingStatus.Unmatched;
            r.Method    = MappingMethod.None;
            r.Score     = 0;
            r.IsIgnored = false;
        }
        OnPropertyChanged(nameof(CanProceed));
    }

    [RelayCommand]
    private void SaveMapping()
    {
        if (_context.SelectedTable is null) return;
        var dict = MappingRows
            .Where(r => !r.IsIgnored && r.SelectedCsvColumn is not null)
            .ToDictionary(r => r.SelectedCsvColumn!.Name, r => r.DbColumn.Name);
        _settings.Current.Mapping.SavedMappings[_context.SelectedTable] = dict;
        _settings.Save();
    }

    private async Task RunAutoMatchAsync()
    {
        MappingRows.Clear();
        if (_context.DbColumns.Count == 0 || CsvColumns.Count == 0) return;

        var resolver = new SynonymResolver(_settings.Current.Mapping.Synonyms);
        var engine   = new MappingEngine(
            resolver,
            _settings.Current.Mapping.FuzzyThreshold,
            _settings.Current.Mapping.VectorThreshold);

        Dictionary<string, string>? saved = null;
        if (_context.SelectedTable is not null &&
            _settings.Current.Mapping.SavedMappings.TryGetValue(_context.SelectedTable, out var sm))
            saved = sm;

        var engineResult = await engine.RunAsync(CsvColumns, _context.DbColumns, saved);

        foreach (var dbCol in _context.DbColumns)
        {
            var match = engineResult.FirstOrDefault(m => m.Target?.Name == dbCol.Name);
            var row   = new MappingRow { DbColumn = dbCol };
            if (match is not null)
            {
                row.SelectedCsvColumn = match.Source;
                row.Status            = match.Status;
                row.Method            = match.Method;
                row.Score             = match.Score;
            }
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is "SelectedCsvColumn" or "IsIgnored")
                    OnPropertyChanged(nameof(CanProceed));
            };
            MappingRows.Add(row);
        }

        _context.MappingRows = MappingRows.ToList();
        OnPropertyChanged(nameof(CanProceed));
    }

    private List<CsvColumn> BuildCsvColumns()
    {
        var headers  = _context.CsvHeaders;
        var previews = _context.PreviewRows;

        return headers.Select((name, idx) => new CsvColumn
        {
            Index          = idx,
            Name           = name,
            NormalizedName = name.ToUpperInvariant(),
            SampleValues   = previews.Select(r => idx < r.Length ? r[idx] : string.Empty).ToList(),
            InferredType   = InferType(previews.Select(r => idx < r.Length ? r[idx] : string.Empty).ToArray()),
        }).ToList();
    }

    private static InferredDataType InferType(string[] samples)
    {
        var nonEmpty = samples.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (nonEmpty.Length == 0) return InferredDataType.Unknown;
        if (nonEmpty.All(s => int.TryParse(s, out _)))
            return InferredDataType.Integer;
        if (nonEmpty.All(s => decimal.TryParse(s,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _)))
            return InferredDataType.Decimal;
        if (nonEmpty.All(s => DateTime.TryParse(s, out _)))
            return InferredDataType.DateTime;
        if (nonEmpty.All(s => s is "0" or "1" or "true" or "false" or "yes" or "no"))
            return InferredDataType.Boolean;
        return InferredDataType.Text;
    }
}
