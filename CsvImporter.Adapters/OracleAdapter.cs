using System.Data;
using CsvImporter.Core.Interfaces;
using CsvImporter.Core.Models;
using Oracle.ManagedDataAccess.Client;

namespace CsvImporter.Adapters;

public sealed class OracleAdapter : IDbAdapter
{
    private OracleConnection? _connection;

    public DbProvider Provider => DbProvider.Oracle;

    private static string BuildConnectionString(ConnectionProfile p)
    {
        var role = p.Role switch
        {
            DbRole.SysDba  => "SYSDBA",
            DbRole.SysOper => "SYSOPER",
            _              => string.Empty
        };
        var cs = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={p.Host})(PORT={p.Port}))(CONNECT_DATA=(SERVICE_NAME={p.ServiceName})));User Id={p.Username};Password={p.Password};";
        if (!string.IsNullOrEmpty(role))
            cs += $"DBA Privilege={role};";
        return cs;
    }

    public async Task ConnectAsync(ConnectionProfile profile, CancellationToken ct = default)
    {
        _connection = new OracleConnection(BuildConnectionString(profile));
        await _connection.OpenAsync(ct);
    }

    public async Task DisconnectAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async Task<bool> TestConnectionAsync(ConnectionProfile profile, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new OracleConnection(BuildConnectionString(profile));
            await conn.OpenAsync(ct);
            return true;
        }
        catch { return false; }
    }

    public async Task<List<string>> GetTablesAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        var tables = new List<string>();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT TABLE_NAME FROM ALL_TABLES ORDER BY TABLE_NAME";
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            tables.Add(reader.GetString(0));
        return tables;
    }

    public async Task<List<DbColumn>> GetTableSchemaAsync(string tableName, CancellationToken ct = default)
    {
        EnsureConnected();
        var cols = new List<DbColumn>();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            SELECT COLUMN_NAME, DATA_TYPE, NULLABLE, DATA_LENGTH, DATA_PRECISION, DATA_SCALE
            FROM ALL_TAB_COLUMNS
            WHERE TABLE_NAME = :tn
            ORDER BY COLUMN_ID
            """;
        cmd.Parameters.Add(new OracleParameter("tn", tableName.ToUpperInvariant()));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            cols.Add(new DbColumn
            {
                Name           = reader.GetString(0),
                NormalizedName = reader.GetString(0),
                DataType       = reader.GetString(1),
                IsNullable     = reader.GetString(2) == "Y",
                MaxLength      = reader.IsDBNull(3) ? null : (int?)reader.GetDecimal(3),
                Precision      = reader.IsDBNull(4) ? null : (int?)reader.GetDecimal(4),
                Scale          = reader.IsDBNull(5) ? null : (int?)reader.GetDecimal(5),
            });
        }
        return cols;
    }

    public Task<IDbTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        return Task.FromResult<IDbTransaction>(_connection!.BeginTransaction());
    }

    public async Task ExecuteBatchAsync(List<string[]> rows, List<ColumnMapping> mappings,
                                        string targetTable, IDbTransaction tx, CancellationToken ct = default)
    {
        EnsureConnected();
        var activeMappings = mappings.Where(m => !m.IsIgnored && m.Target is not null).ToList();
        if (activeMappings.Count == 0 || rows.Count == 0) return;

        var cols   = string.Join(", ", activeMappings.Select(m => m.Target!.Name));
        var pars   = string.Join(", ", activeMappings.Select((_, i) => $":p{i}"));
        var sql    = $"INSERT INTO {targetTable} ({cols}) VALUES ({pars})";

        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText  = sql;
        cmd.Transaction  = (OracleTransaction)tx;

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            cmd.Parameters.Clear();
            for (int i = 0; i < activeMappings.Count; i++)
            {
                var srcIdx = activeMappings[i].Source.Index;
                var val    = srcIdx < row.Length ? row[srcIdx] : string.Empty;
                cmd.Parameters.Add(new OracleParameter($"p{i}", (object?)val ?? DBNull.Value));
            }
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public Task CommitAsync(IDbTransaction tx) { tx.Commit(); return Task.CompletedTask; }
    public Task RollbackAsync(IDbTransaction tx) { tx.Rollback(); return Task.CompletedTask; }

    private void EnsureConnected()
    {
        if (_connection is null || _connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
