using System.Data;
using CsvImporter.Core.Interfaces;
using CsvImporter.Core.Models;
using Npgsql;

namespace CsvImporter.Adapters;

public sealed class PostgresAdapter : IDbAdapter
{
    private NpgsqlConnection? _connection;

    public DbProvider Provider => DbProvider.Postgres;

    private static string BuildConnectionString(ConnectionProfile p)
        => $"Host={p.Host};Port={p.Port};Database={p.DbName};Username={p.Username};Password={p.Password};";

    public async Task ConnectAsync(ConnectionProfile profile, CancellationToken ct = default)
    {
        _connection = new NpgsqlConnection(BuildConnectionString(profile));
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
            await using var conn = new NpgsqlConnection(BuildConnectionString(profile));
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
        cmd.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name";
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
            SELECT c.column_name, c.data_type, c.is_nullable,
                   c.character_maximum_length, c.numeric_precision, c.numeric_scale,
                   CASE WHEN kcu.column_name IS NOT NULL THEN true ELSE false END AS is_pk
            FROM information_schema.columns c
            LEFT JOIN information_schema.key_column_usage kcu
                ON c.table_name = kcu.table_name AND c.column_name = kcu.column_name
               AND (SELECT constraint_type FROM information_schema.table_constraints tc
                    WHERE tc.constraint_name = kcu.constraint_name LIMIT 1) = 'PRIMARY KEY'
            WHERE c.table_name = @tn AND c.table_schema = 'public'
            ORDER BY c.ordinal_position
            """;
        cmd.Parameters.AddWithValue("tn", tableName.ToLowerInvariant());
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
                Precision      = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4),
                Scale          = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                IsPrimaryKey   = !reader.IsDBNull(6) && reader.GetBoolean(6),
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

        var cols = string.Join(", ", activeMappings.Select(m => $"\"{m.Target!.Name}\""));
        var pars = string.Join(", ", activeMappings.Select((_, i) => $"@p{i}"));
        var sql  = $"INSERT INTO \"{targetTable}\" ({cols}) VALUES ({pars})";

        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText  = sql;
        cmd.Transaction  = (NpgsqlTransaction)tx;

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            cmd.Parameters.Clear();
            for (int i = 0; i < activeMappings.Count; i++)
            {
                var srcIdx = activeMappings[i].Source.Index;
                var val    = srcIdx < row.Length ? row[srcIdx] : string.Empty;
                cmd.Parameters.AddWithValue($"p{i}", (object?)val ?? DBNull.Value);
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
