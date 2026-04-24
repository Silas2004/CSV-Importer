using System.Text;
using CsvImporter.Core.Interfaces;
using CsvImporter.Core.Models;

namespace CsvImporter.FileSystem.Strategy;

public class SequentialStrategy : IImportStrategy
{
    private readonly int _batchSize;

    public SequentialStrategy(int batchSize = 100)
    {
        _batchSize = batchSize;
    }

    public ImportStrategy StrategyType => ImportStrategy.Sequential;

    public IEnumerable<List<string[]>> ReadBatches(string filePath, string delimiter, Encoding encoding)
    {
        using var reader = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks: true);
        reader.ReadLine(); // skip header

        var batch = new List<string[]>(_batchSize);
        while (reader.ReadLine() is { } line)
        {
            //TODO: Change in Settings
            batch.Add(line.Split(delimiter).Select(v => v.Trim(',')).ToArray());
            if (batch.Count >= _batchSize)
            {
                yield return batch;
                batch = new List<string[]>(_batchSize);
            }
        }
        if (batch.Count > 0)
            yield return batch;
    }
}
