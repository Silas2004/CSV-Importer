using CsvImporter.Core.Interfaces;
using CsvImporter.Core.Models;

namespace CsvImporter.Adapters;

public static class DbAdapterFactory
{
    public static IDbAdapter Create(DbProvider provider) => provider switch
    {
        DbProvider.Oracle   => new OracleAdapter(),
        DbProvider.MsSql    => new MsSqlAdapter(),
        DbProvider.Postgres => new PostgresAdapter(),
        _ => throw new NotSupportedException($"Provider {provider} is not supported.")
    };
}
