namespace CsvImporter.Core.Models;

public class ColumnMapping
{
    public CsvColumn   Source     { get; set; } = null!;
    public DbColumn?   Target     { get; set; }
    public MappingStatus Status   { get; set; } = MappingStatus.Unmatched;
    public MappingMethod Method   { get; set; } = MappingMethod.None;
    public double       Score     { get; set; } = 0.0;
    public bool         IsIgnored { get; set; } = false;

    public bool HasTypeWarning => ComputeTypeWarning();

    private bool ComputeTypeWarning()
    {
        if (Target is null || Source.InferredType == InferredDataType.Unknown)
            return false;

        var dt = Target.DataType.ToUpperInvariant();
        return Source.InferredType switch
        {
            InferredDataType.Integer => !dt.Contains("INT") && !dt.Contains("NUMBER") && !dt.Contains("NUMERIC") && !dt.Contains("DECIMAL"),
            InferredDataType.Decimal => !dt.Contains("FLOAT") && !dt.Contains("REAL") && !dt.Contains("NUMBER") && !dt.Contains("NUMERIC") && !dt.Contains("DECIMAL") && !dt.Contains("MONEY"),
            InferredDataType.Date or InferredDataType.DateTime => !dt.Contains("DATE") && !dt.Contains("TIME") && !dt.Contains("TIMESTAMP"),
            InferredDataType.Boolean => !dt.Contains("BOOL") && !dt.Contains("BIT") && !dt.Contains("CHAR"),
            _ => false
        };
    }
}

public enum MappingStatus { Unmatched, Warned, Matched }
public enum MappingMethod { None, ExactMatch, NormalizedMatch, TokenMatch, Synonym, Fuzzy, Vector, Manual }
