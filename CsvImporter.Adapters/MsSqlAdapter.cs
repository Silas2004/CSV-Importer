using System.Data;
using CsvImporter.Core.Interfaces;
using CsvImporter.Core.Models;
using Microsoft.Data.SqlClient;

namespace CsvImporter.Adapters;

public sealed class MsSqlAdapter : IDbAdapter
{
    private SqlConnection? _connection;

    public DbProvider Provider => DbProvider.MsSql;

    private static string BuildConnectionString(ConnectionProfile p)
        => $"Server={p.Host},{p.Port};Database={p.DbName};User Id={p.Username};Password={p.Password};TrustServerCertificate=True;";

    public async Task ConnectAsync(ConnectionProfile profile, CancellationToken ct = default)
    {
        _connection = new SqlConnection(BuildConnectionString(profile));
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
            await using var conn = new SqlConnection(BuildConnectionString(profile));
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
        cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME";
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
            SELECT c.COLUMN_NAME, c.DATA_TYPE, c.IS_NULLABLE,
                   c.CHARACTER_MAXIMUM_LENGTH, c.NUMERIC_PRECISION, c.NUMERIC_SCALE,
                   CASE WHEN kcu.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PK
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                ON c.TABLE_NAME = kcu.TABLE_NAME AND c.COLUMN_NAME = kcu.COLUMN_NAME
               AND OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME),'IsPrimaryKey') = 1
            WHERE c.TABLE_NAME = @tn
            ORDER BY c.ORDINAL_POSITION
            """;
        cmd.Parameters.AddWithValue("@tn", tableName);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            cols.Add(new DbColumn
            {
                Name           = reader.GetString(0),
                NormalizedName = reader.GetString(0),
                DataType       = reader.GetString(1),
                IsNullable     = reader.GetString(2) == "YES",
                MaxLength      = reader.IsDBNull(3) ? null : (int?)reader.GetInt32(3),
                Precision      = reader.IsDBNull(4) ? null : (int?)reader.GetByte(4),
                Scale          = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                IsPrimaryKey   = reader.GetInt32(6) == 1,
            });
        }
        return cols;
    }

    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        return await _connection!.BeginTransactionAsync(ct);
    }

    public async Task ExecuteBatchAsync(List<string[]> rows, List<ColumnMapping> mappings,
                                        string targetTable, IDbTransaction tx, CancellationToken ct = default)
    {
        EnsureConnected();
        var activeMappings = mappings.Where(m => !m.IsIgnored && m.Target is not null).ToList();
        if (activeMappings.Count == 0 || rows.Count == 0) return;

        var dt = new DataTable();
        foreach (var m in activeMappings)
            dt.Columns.Add(m.Target!.Name);

        foreach (var row in rows)
        {
            var dr = dt.NewRow();
            for (int i = 0; i < activeMappings.Count; i++)
            {
                var srcIdx  = activeMappings[i].Source.Index;
                dr[i] = srcIdx < row.Length ? (object)row[srcIdx] : DBNull.Value;
            }
            dt.Rows.Add(dr);
        }

        using var bulk = new SqlBulkCopy(_connection, SqlBulkCopyOptions.Default, (SqlTransaction)tx);
        bulk.DestinationTableName = targetTable;
        foreach (DataColumn col in dt.Columns)
            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
        await bulk.WriteToServerAsync(dt, ct);
    }

    public Task CommitAsync(IDbTransaction tx) { tx.Commit(); return Task.CompletedTask; }
    public Task RollbackAsync(IDbTransaction tx) { tx.Rollback(); return Task.CompletedTask; }

    private void EnsureConnected()
    {
        if (_connection is null || _connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();

    public async Task<List<Dictionary<string, object?>>> QueryAsync(string sql, CancellationToken ct = default)
    {
        EnsureConnected();
        var rows = new List<Dictionary<string, object?>>();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            rows.Add(row);
        }
        return rows;
    }
}
