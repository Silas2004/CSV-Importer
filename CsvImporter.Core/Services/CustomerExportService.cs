using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvImporter.Core.Models;
using ProtoBuf;

namespace CsvImporter.Core.Services;

public class CustomerExportService
{
    public CustomerExportService()
    {
    }

    public async Task<List<Customer>> ReadCustomersAsync(CsvImporter.Core.Interfaces.IDbAdapter adapter, string tableName)
    {
        // Attempt to select common customer columns. This is a simple heuristic.
        var sql = $"SELECT * FROM {tableName} LIMIT 1000"; // LIMIT works for Postgres/MySQL; SQL Server/Oracle will ignore - hosting apps should pass provider-specific table names.
        var rows = await adapter.QueryAsync(sql);

        var customers = rows.Select(r => new Customer
        {
            Id = TryGetLong(r, "id") ?? 0,
            FirstName = TryGetString(r, "first_name") ?? TryGetString(r, "firstname") ?? string.Empty,
            LastName = TryGetString(r, "last_name") ?? TryGetString(r, "lastname") ?? string.Empty,
            Email = TryGetString(r, "email") ?? string.Empty,
            Phone = TryGetString(r, "phone") ?? TryGetString(r, "telephone") ?? string.Empty,
            BirthDate = TryGetDate(r, "birthdate")
        }).ToList();

        return customers;
    }

    private static string? TryGetString(Dictionary<string, object?> row, string key)
    {
        var k = row.Keys.FirstOrDefault(x => string.Equals(x, key, System.StringComparison.OrdinalIgnoreCase));
        if (k is null) return null;
        return row[k]?.ToString();
    }

    private static long? TryGetLong(Dictionary<string, object?> row, string key)
    {
        var s = TryGetString(row, key);
        if (long.TryParse(s, out var v)) return v;
        return null;
    }

    private static DateTime? TryGetDate(Dictionary<string, object?> row, string key)
    {
        var s = TryGetString(row, key);
        if (DateTime.TryParse(s, out var dt)) return dt;
        return null;
    }

    public void ExportToProtobuf(IEnumerable<Customer> customers, Stream outStream)
    {
        Serializer.Serialize(outStream, customers.ToList());
    }

    public byte[] ExportToProtobuf(IEnumerable<Customer> customers)
    {
        using var ms = new MemoryStream();
        ExportToProtobuf(customers, ms);
        return ms.ToArray();
    }
}
