using CommunityToolkit.Mvvm.ComponentModel;
using CsvImporter.Core.Models;
using CsvImporter.Core.Services;

namespace CsvImporter.WPF.ViewModels;

public partial class ImportConfigViewModel : ObservableObject
{
    [ObservableProperty] private TransactionMode _txMode       = TransactionMode.Batch;
    [ObservableProperty] private int             _batchSize    = 100;
    [ObservableProperty] private ErrorBehavior   _onError      = ErrorBehavior.Skip;
    [ObservableProperty] private string          _encoding     = "UTF-8";
    [ObservableProperty] private string          _delimiter    = ";";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBatchSize))]
    private TransactionMode _selectedTxMode = TransactionMode.Batch;

    public bool ShowBatchSize => SelectedTxMode == TransactionMode.Batch;

    public List<string> EncodingOptions { get; } = new() { "UTF-8", "ISO-8859-1", "UTF-16" };
    public List<string> DelimiterOptions { get; } = new() { ";", ",", "\\t" };

    public ImportConfigViewModel(AppSettingsService settings)
    {
        var imp  = settings.Current.Import;
        BatchSize = imp.DefaultBatchSize;
        Encoding  = imp.DefaultEncoding;
        Delimiter = imp.DefaultDelimiter;

        SelectedTxMode = imp.DefaultTxMode switch
        {
            "AllOrNothing" => TransactionMode.AllOrNothing,
            "RowByRow"     => TransactionMode.RowByRow,
            _              => TransactionMode.Batch,
        };
        OnError = imp.DefaultErrorBehavior switch
        {
            "Abort"   => ErrorBehavior.Abort,
            "Collect" => ErrorBehavior.Collect,
            _         => ErrorBehavior.Skip,
        };
    }

    public void ApplyTo(IEnumerable<CsvImporter.Core.Models.ImportJob> jobs)
    {
        foreach (var job in jobs)
        {
            job.TxMode    = SelectedTxMode;
            job.BatchSize = BatchSize;
            job.OnError   = OnError;
        }
    }
}
