using System.Data;
using CsvImporter.Core.Models;

namespace CsvImporter.Core.Interfaces;

public interface IDbAdapter : IAsyncDisposable
{
    DbProvider Provider { get; }

    Task ConnectAsync(ConnectionProfile profile, CancellationToken ct = default);
    Task DisconnectAsync();
    Task<bool> TestConnectionAsync(ConnectionProfile profile, CancellationToken ct = default);
    Task<List<string>>   GetTablesAsync(CancellationToken ct = default);
    Task<List<DbColumn>> GetTableSchemaAsync(string tableName, CancellationToken ct = default);
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task ExecuteBatchAsync(List<string[]> rows, List<ColumnMapping> mappings,
                           string targetTable, IDbTransaction tx, CancellationToken ct = default);
    Task CommitAsync(IDbTransaction tx);
    Task RollbackAsync(IDbTransaction tx);
}
