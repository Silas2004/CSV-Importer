namespace CsvImporter.Core.Models;

public class CsvColumn
{
    public int              Index          { get; set; }
    public string           Name           { get; set; } = string.Empty;
    public string           NormalizedName { get; set; } = string.Empty;
    public List<string>     SampleValues   { get; set; } = new();
    public InferredDataType InferredType   { get; set; } = InferredDataType.Unknown;
    public bool             IsIgnored      { get; set; } = false;
}

public enum InferredDataType { Unknown, Text, Integer, Decimal, Date, DateTime, Boolean }
