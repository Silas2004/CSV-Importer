using System.IO;
using CsvImporter.Core.Models;
using CsvImporter.FileSystem.Strategy;

namespace CsvImporter.Tests;

public class ImportStrategyResolverTests
{
    private const long Threshold = 10 * 1024 * 1024; // 10 MB

    [Fact]
    public void Resolve_Auto_SmallFile_UsesWholeFile()
    {
        var resolver = new ImportStrategyResolver(Threshold);
        var job = new ImportJob
        {
            FilePath      = "small.csv",
            FileSizeBytes = 9 * 1024 * 1024, // 9 MB < threshold
            Strategy      = ImportStrategy.Auto,
        };

        var strategy = resolver.Resolve(job);

        Assert.Equal(ImportStrategy.WholeFile,  strategy.StrategyType);
        Assert.Equal(ImportStrategy.WholeFile, job.ResolvedStrategy);
    }

    [Fact]
    public void Resolve_Auto_LargeFile_UsesSequential()
    {
        var resolver = new ImportStrategyResolver(Threshold);
        var job = new ImportJob
        {
            FilePath      = "large.csv",
            FileSizeBytes = 11 * 1024 * 1024, // 11 MB > threshold
            Strategy      = ImportStrategy.Auto,
        };

        var strategy = resolver.Resolve(job);

        Assert.Equal(ImportStrategy.Sequential, strategy.StrategyType);
        Assert.Equal(ImportStrategy.Sequential, job.ResolvedStrategy);
    }

    [Fact]
    public void Resolve_ForcedWholeFile_IgnoresSize()
    {
        var resolver = new ImportStrategyResolver(Threshold);
        var job = new ImportJob
        {
            FilePath      = "huge.csv",
            FileSizeBytes = 100 * 1024 * 1024,
            Strategy      = ImportStrategy.WholeFile,
        };

        var strategy = resolver.Resolve(job);

        Assert.Equal(ImportStrategy.WholeFile, strategy.StrategyType);
    }

    [Fact]
    public void Resolve_ForcedSequential_IgnoresSize()
    {
        var resolver = new ImportStrategyResolver(Threshold);
        var job = new ImportJob
        {
            FilePath      = "tiny.csv",
            FileSizeBytes = 1024,
            Strategy      = ImportStrategy.Sequential,
        };

        var strategy = resolver.Resolve(job);

        Assert.Equal(ImportStrategy.Sequential, strategy.StrategyType);
    }
}
