using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsvImporter.Adapters;
using CsvImporter.Core.Models;
using CsvImporter.Core.Services;
using CsvImporter.WPF.Models;
using Microsoft.Win32;

namespace CsvImporter.WPF.ViewModels;

public partial class TableSelectionViewModel : ObservableObject, IWizardStep
{
    private readonly ImportContext _context;
    private readonly CustomerExportService _exportService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanProceed))]
    private List<string> _tables = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanProceed))]
    private string? _selectedTable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanProceed))]
    private bool _schemaLoaded;

    [ObservableProperty] private bool   _isLoadingTables;
    [ObservableProperty] private bool   _isLoadingSchema;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => ErrorMessage is not null;

    public List<DbColumn> DbColumns => _context.DbColumns;

    public bool CanProceed => SelectedTable is not null && SchemaLoaded;

    public string ConnectionInfo =>
        string.IsNullOrWhiteSpace(_context.Profile.Host)
            ? "Keine Verbindung — bitte zuerst ⚙ Einstellungen öffnen."
            : $"{_context.Profile.Provider}: {_context.Profile.Host}";

    public TableSelectionViewModel(ImportContext context, CustomerExportService exportService)
    {
        _context = context;
        _exportService = exportService;
    }

    public async Task EnterAsync()
    {
        ErrorMessage = null;
        SchemaLoaded = false;
        OnPropertyChanged(nameof(ConnectionInfo));
        await LoadTablesAsync();
    }

    [RelayCommand]
    private async Task LoadTablesAsync()
    {
        if (string.IsNullOrWhiteSpace(_context.Profile.Host)) return;
        IsLoadingTables = true;
        ErrorMessage    = null;
        try
        {
            await using var adapter = DbAdapterFactory.Create(_context.Profile.Provider);
            await adapter.ConnectAsync(_context.Profile);
            Tables = await adapter.GetTablesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Tabellen konnten nicht geladen werden: {ex.Message}";
        }
        finally
        {
            IsLoadingTables = false;
        }
    }

    partial void OnSelectedTableChanged(string? value)
    {
        if (value is null) return;
        _ = LoadSchemaAsync(value);
    }

    private async Task LoadSchemaAsync(string table)
    {
        IsLoadingSchema = true;
        SchemaLoaded    = false;
        _context.DbColumns = new();
        OnPropertyChanged(nameof(DbColumns));
        ErrorMessage = null;
        try
        {
            await using var adapter = DbAdapterFactory.Create(_context.Profile.Provider);
            await adapter.ConnectAsync(_context.Profile);
            _context.DbColumns   = await adapter.GetTableSchemaAsync(table);
            _context.SelectedTable = table;
            SchemaLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Schema konnte nicht geladen werden: {ex.Message}";
        }
        finally
        {
            IsLoadingSchema = false;
            OnPropertyChanged(nameof(DbColumns));
            OnPropertyChanged(nameof(CanProceed));
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedTable)) return;

        try
        {
            var dlg = new SaveFileDialog { Filter = "Protobuf files (*.pb)|*.pb|All files (*.*)|*.*", FileName = "customers.pb" };
            var res = dlg.ShowDialog();
            if (res != true) return;

            await using var adapter = DbAdapterFactory.Create(_context.Profile.Provider);
            await adapter.ConnectAsync(_context.Profile);

            var customers = await _exportService.ReadCustomersAsync(adapter, SelectedTable!);

            await using var fs = File.Create(dlg.FileName!);
            _exportService.ExportToProtobuf(customers, fs);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export fehlgeschlagen: {ex.Message}";
        }
    }
}
