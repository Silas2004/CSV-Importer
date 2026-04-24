namespace CsvImporter.Core.Models;

public class ImportResult
{
    public int            JobId         { get; set; }
    public string         FilePath      { get; set; } = string.Empty;
    public string         TargetTable   { get; set; } = string.Empty;
    public ImportStrategy StrategyUsed  { get; set; }
    public bool           Success       { get; set; }
    public int            RowsTotal     { get; set; }
    public int            RowsCommitted { get; set; }
    public int            RowsSkipped   { get; set; }
    public int            RowsFailed    { get; set; }
    public DateTime       StartedAt     { get; set; }
    public DateTime       FinishedAt    { get; set; }
    public TimeSpan       Duration      => FinishedAt - StartedAt;
    public List<RowError> Errors        { get; set; } = new();
}

public class RowError
{
    public int    RowIndex { get; set; }
    public string Column   { get; set; } = string.Empty;
    public string Value    { get; set; } = string.Empty;
    public string Message  { get; set; } = string.Empty;
}
