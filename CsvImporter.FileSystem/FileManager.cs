using System.Text;

namespace CsvImporter.FileSystem;

public class FileManager : IDisposable
{
    private StreamWriter? _logWriter;

    public List<FileInfo> ScanDirectory(string path, bool recursive)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(path, "*.csv", option)
                        .Select(f => new FileInfo(f))
                        .ToList();
    }

    public StreamReader OpenForReading(string path, Encoding encoding)
        => new StreamReader(path, encoding, detectEncodingFromByteOrderMarks: true);

    public IEnumerable<string> ReadLines(string path, Encoding encoding)
    {
        using var reader = OpenForReading(path, encoding);
        while (reader.ReadLine() is { } line)
            yield return line;
    }

    public Encoding DetectEncoding(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        Span<byte> bom = stackalloc byte[4];
        int read = fs.Read(bom);
        if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            return Encoding.UTF8;
        if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
            return Encoding.Unicode;
        if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
            return Encoding.BigEndianUnicode;
        return Encoding.UTF8;
    }

    public (int FileCount, long TotalBytes) GetDirectoryInfo(string path)
    {
        var files = Directory.EnumerateFiles(path, "*.csv", SearchOption.AllDirectories).ToList();
        long total = files.Sum(f => new FileInfo(f).Length);
        return (files.Count, total);
    }

    public void OpenLog(string fileName)
    {
        _logWriter = new StreamWriter(fileName, append: true, Encoding.UTF8) { AutoFlush = true };
    }

    public void WriteLog(string msg)
        => _logWriter?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}");

    public void CloseLog()
    {
        _logWriter?.Dispose();
        _logWriter = null;
    }

    public void Dispose() => CloseLog();
}
