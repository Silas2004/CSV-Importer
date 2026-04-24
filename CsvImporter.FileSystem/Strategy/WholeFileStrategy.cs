using System.Text;
using CsvImporter.Core.Interfaces;
using CsvImporter.Core.Models;

namespace CsvImporter.FileSystem.Strategy;

public class WholeFileStrategy : IImportStrategy
{
    public ImportStrategy StrategyType => ImportStrategy.WholeFile;

    public IEnumerable<List<string[]>> ReadBatches(string filePath, string delimiter, Encoding encoding)
    {
        var rows = new List<string[]>();
        using var reader = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks: true);
        reader.ReadLine(); // skip header
        while (reader.ReadLine() is { } line)
            //TODO: Change in Settings
            rows.Add(line.Split(delimiter).Select(v => v.Trim(',')).ToArray());
        yield return rows;
    }
}
