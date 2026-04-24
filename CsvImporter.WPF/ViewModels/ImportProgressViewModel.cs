using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsvImporter.Adapters;
using CsvImporter.Core.Models;
using CsvImporter.Core.Services;
using CsvImporter.FileSystem;
using CsvImporter.FileSystem.Strategy;
using CsvImporter.WPF.Models;

namespace CsvImporter.WPF.ViewModels;

public partial class ImportProgressViewModel : ObservableObject, IWizardStep
{
    private readonly ImportContext      _context;
    private readonly AppSettingsService _settings;
    private readonly ImportQueue        _queue;
    private CancellationTokenSource?   _cts;

    // ── Inline import config ──────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBatchSize))]
    private TransactionMode _txMode = TransactionMode.AllOrNothing;

    [ObservableProperty] private ErrorBehavior _onError   = ErrorBehavior.Abort;
    [ObservableProperty] private int           _batchSize = 100;

    public bool ShowBatchSize => TxMode == TransactionMode.Batch;

    // ── Progress ──────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private string _globalLog     = string.Empty;
    [ObservableProperty] private int    _totalCommitted;
    [ObservableProperty] private int    _totalSkipped;
    [ObservableProperty] private int    _totalFailed;

    public ObservableCollection<ImportJob> Jobs { get; } = new();

    private readonly StringBuilder _logBuffer = new();
    private readonly object        _logLock   = new();

    public ImportProgressViewModel(ImportContext context, AppSettingsService settings, ImportQueue queue)
    {
        _context  = context;
        _settings = settings;
        _queue    = queue;
    }

    public bool CanProceed => true;

    public Task EnterAsync()
    {
        TxMode    = _context.TxMode;
        OnError   = _context.OnError;
        BatchSize = _context.BatchSize;

        Jobs.Clear();
        TotalCommitted = 0; TotalSkipped = 0; TotalFailed = 0;
        _logBuffer.Clear();
        GlobalLog = string.Empty;

        var resolver = new ImportStrategyResolver(_settings.Current.Import.SizeThresholdBytes);
        var mappings = BuildColumnMappings();

        foreach (var path in _context.FilePaths)
        {
            var info = new System.IO.FileInfo(path);
            var job  = new ImportJob
            {
                FilePath      = path,
                TargetTable   = _context.SelectedTable ?? string.Empty,
                FileSizeBytes = info.Length,
                BatchSize     = BatchSize,
                TxMode        = TxMode,
                OnError       = OnError,
                Strategy      = ImportStrategy.Auto,
                Mappings      = mappings,
            };
            job.ResolvedStrategy = resolver.Peek(path);
            _queue.Enqueue(job);
            Jobs.Add(job);
        }
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartImportAsync()
    {
        foreach (var job in Jobs)
        {
            job.TxMode    = TxMode;
            job.OnError   = OnError;
            job.BatchSize = BatchSize;
        }

        _cts      = new CancellationTokenSource();
        IsRunning = true;
        StartImportCommand.NotifyCanExecuteChanged();

        var progress = new Progress<ImportProgress>(OnProgress);
        var resolver = new ImportStrategyResolver(_settings.Current.Import.SizeThresholdBytes);
        var orchestrator = new ImportOrchestrator(
            provider => DbAdapterFactory.Create(provider),
            job      => resolver.Resolve(job),
            _settings.Current.Import.MaxParallelImports,
            _settings.Current.Import.SizeThresholdBytes);

        try
        {
            await orchestrator.RunAsync(Jobs, _context.Profile, progress, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Import abgebrochen.");
        }
        finally
        {
            IsRunning = false;
            StartImportCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    private bool CanStart() => !IsRunning && Jobs.Count > 0;

    private void OnProgress(ImportProgress p)
    {
        var job = Jobs.FirstOrDefault(j => j.Id == p.JobId);
        if (job is not null)
        {
            job.RowsDone   = p.RowsDone;
            job.RowsTotal  = p.RowsTotal;
            job.RowsFailed = p.RowsFailed;
            if (p.IsComplete)
                job.Status = p.Message.StartsWith("Failed") ? JobStatus.Failed : JobStatus.Done;
        }
        AppendLog($"[Job {p.JobId}] {p.Message}");
        TotalCommitted = Jobs.Sum(j => j.RowsDone);
        TotalFailed    = Jobs.Sum(j => j.RowsFailed);
    }

    private void AppendLog(string msg)
    {
        lock (_logLock)
        {
            _logBuffer.AppendLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
            var lines = _logBuffer.ToString().Split('\n');
            if (lines.Length > 500)
            {
                _logBuffer.Clear();
                _logBuffer.Append(string.Join('\n', lines.TakeLast(500)));
            }
            GlobalLog = _logBuffer.ToString();
        }
    }

    private List<ColumnMapping> BuildColumnMappings() =>
        _context.MappingRows
            .Where(r => !r.IsIgnored && r.SelectedCsvColumn is not null)
            .Select(r => new ColumnMapping
            {
                Source  = r.SelectedCsvColumn!,
                Target  = r.DbColumn,
                Status  = r.Status,
                Method  = r.Method,
                Score   = r.Score,
            }).ToList();
}
