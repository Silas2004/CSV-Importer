using CsvImporter.Core.Models;
using CsvImporter.Core.Interfaces;

namespace CsvImporter.FileSystem.Strategy;

public class ImportStrategyResolver
{
    private readonly long _thresholdBytes;

    public ImportStrategyResolver(long thresholdBytes = 10 * 1024 * 1024)
    {
        _thresholdBytes = thresholdBytes;
    }

    public IImportStrategy Resolve(ImportJob job)
    {
        var resolved = job.Strategy == ImportStrategy.Auto
            ? (job.FileSizeBytes >= _thresholdBytes ? ImportStrategy.Sequential : ImportStrategy.WholeFile)
            : job.Strategy;

        job.ResolvedStrategy = resolved;

        return resolved == ImportStrategy.Sequential
            ? new SequentialStrategy(job.BatchSize)
            : new WholeFileStrategy();
    }

    public ImportStrategy Peek(string filePath)
    {
        var size = new FileInfo(filePath).Length;
        return size >= _thresholdBytes ? ImportStrategy.Sequential : ImportStrategy.WholeFile;
    }
}
