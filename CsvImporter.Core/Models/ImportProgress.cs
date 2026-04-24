namespace CsvImporter.Core.Models;

public class ImportProgress
{
    public int    JobId      { get; set; }
    public int    RowsDone   { get; set; }
    public int    RowsTotal  { get; set; }
    public int    RowsFailed { get; set; }
    public string Message    { get; set; } = string.Empty;
    public bool   IsComplete { get; set; } = false;
}
