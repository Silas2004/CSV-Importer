namespace CsvImporter.Core.Services;

public class AppSettings
{
    public ImportSettings  Import  { get; set; } = new();
    public MappingSettings Mapping { get; set; } = new();
}

public class ImportSettings
{
    public long   SizeThresholdBytes  { get; set; } = 10 * 1024 * 1024;
    public int    DefaultBatchSize     { get; set; } = 100;
    public string DefaultTxMode        { get; set; } = "Batch";
    public string DefaultErrorBehavior { get; set; } = "Skip";
    public string DefaultEncoding      { get; set; } = "UTF-8";
    public string DefaultDelimiter     { get; set; } = ";";
    public int    MaxParallelImports   { get; set; } = 3;
}

public class MappingSettings
{
    public double                              FuzzyThreshold  { get; set; } = 0.75;
    public double                              VectorThreshold { get; set; } = 0.70;
    public Dictionary<string, string>          Synonyms        { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> SavedMappings { get; set; } = new();
}
