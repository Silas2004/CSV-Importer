namespace CsvImporter.Core.Models;

public class DbColumn
{
    public string Name           { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string DataType       { get; set; } = string.Empty;
    public bool   IsNullable     { get; set; } = true;
    public int?   MaxLength      { get; set; }
    public int?   Precision      { get; set; }
    public int?   Scale          { get; set; }
    public bool   IsPrimaryKey   { get; set; } = false;
}
