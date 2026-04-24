namespace CsvImporter.Core.Models;

public static class DbProviderValues
{
    public static readonly IReadOnlyList<DbProvider> All =
        Enum.GetValues<DbProvider>().ToList().AsReadOnly();
}
