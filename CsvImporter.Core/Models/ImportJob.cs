namespace CsvImporter.Core.Models;

public class ImportJob
{
    public int    Id            { get; set; }
    public string FilePath      { get; set; } = string.Empty;
    public string FileName      => Path.GetFileName(FilePath);
    public string TargetTable   { get; set; } = string.Empty;
    public long   FileSizeBytes { get; set; }

    public ImportStrategy  Strategy         { get; set; } = ImportStrategy.Auto;
    public ImportStrategy  ResolvedStrategy { get; set; }
    public TransactionMode TxMode           { get; set; } = TransactionMode.Batch;
    public ErrorBehavior   OnError          { get; set; } = ErrorBehavior.Skip;
    public int             BatchSize        { get; set; } = 100;
    public int             Priority         { get; set; } = 2;

    public JobStatus           Status   { get; set; } = JobStatus.Pending;
    public List<ColumnMapping> Mappings { get; set; } = new();

    public int    RowsTotal    { get; set; }
    public int    RowsDone     { get; set; }
    public int    RowsFailed   { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime  EnqueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt  { get; set; }
    public DateTime? FinishedAt { get; set; }
}

public enum ImportStrategy  { Auto, WholeFile, Sequential }
public enum TransactionMode { AllOrNothing, Batch, RowByRow }
public enum ErrorBehavior   { Abort, Skip, Collect }
public enum JobStatus       { Pending, Running, Done, Failed, Skipped }
