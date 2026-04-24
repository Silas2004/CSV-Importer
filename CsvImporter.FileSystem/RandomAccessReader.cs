using System.Text;

namespace CsvImporter.FileSystem;

public sealed class RandomAccessReader : IDisposable
{
    private readonly FileStream   _fs;
    private readonly StreamReader _reader;
    private long                  _headerEndPosition;
    private string                _delimiter;

    public RandomAccessReader(string path, Encoding encoding, string delimiter = ",")
    {
        _delimiter = delimiter;
        _fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        _reader = new StreamReader(_fs, encoding, detectEncodingFromByteOrderMarks: true);
    }

    public List<string> ReadHeader()
    {
        _fs.Seek(0, SeekOrigin.Begin);
        _reader.DiscardBufferedData();
        var line = _reader.ReadLine() ?? string.Empty;
        _headerEndPosition = _fs.Position;
        return Split(line);
    }

    public List<string[]> ReadPreviewRows(int n)
    {
        _fs.Seek(_headerEndPosition, SeekOrigin.Begin);
        _reader.DiscardBufferedData();
        var rows = new List<string[]>();
        for (int i = 0; i < n; i++)
        {
            var line = _reader.ReadLine();
            if (line is null) break;
            rows.Add(Split(line).ToArray());
        }
        return rows;
    }

    public (List<string[]> Rows, long NextPosition) ReadBatch(long startPosition, int size)
    {
        _fs.Seek(startPosition, SeekOrigin.Begin);
        _reader.DiscardBufferedData();
        var rows = new List<string[]>(size);
        for (int i = 0; i < size; i++)
        {
            var line = _reader.ReadLine();
            if (line is null) break;
            rows.Add(Split(line).ToArray());
        }
        return (rows, _fs.Position);
    }

    public int CountDataRows()
    {
        _fs.Seek(_headerEndPosition, SeekOrigin.Begin);
        _reader.DiscardBufferedData();
        int count = 0;
        while (_reader.ReadLine() is not null) count++;
        return count;
    }

    public long HeaderEndPosition => _headerEndPosition;

    private List<string> Split(string line)
        //TODO: Change in Settings
        => line.Split(_delimiter).Select(v => v.Trim('"')).ToList();

    public void Dispose()
    {
        _reader.Dispose();
        _fs.Dispose();
    }
}
