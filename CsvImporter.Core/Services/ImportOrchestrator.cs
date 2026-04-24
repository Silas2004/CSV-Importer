using System.Data;
using CsvImporter.Core.Interfaces;
using CsvImporter.Core.Models;

namespace CsvImporter.Core.Services;

public class ImportOrchestrator
{
    private readonly Func<DbProvider, IDbAdapter>     _adapterFactory;
    private readonly Func<ImportJob, IImportStrategy> _strategyFactory;
    private readonly SemaphoreSlim                    _semaphore;
    private readonly long                             _largeFileThreshold;

    public ImportOrchestrator(
        Func<DbProvider, IDbAdapter>     adapterFactory,
        Func<ImportJob, IImportStrategy> strategyFactory,
        int  maxParallel        = 3,
        long largeFileThreshold = 10 * 1024 * 1024)
    {
        _adapterFactory     = adapterFactory;
        _strategyFactory    = strategyFactory;
        _semaphore          = new SemaphoreSlim(maxParallel, maxParallel);
        _largeFileThreshold = largeFileThreshold;
    }

    public async Task RunAsync(
        IEnumerable<ImportJob>    jobs,
        ConnectionProfile         profile,
        IProgress<ImportProgress> progress,
        CancellationToken         ct = default)
    {
        var tasks = new List<Task>();
        foreach (var job in jobs)
        {
            await _semaphore.WaitAsync(ct);
            var captured = job;
            Task t = captured.FileSizeBytes >= _largeFileThreshold
                ? Task.Run(() => RunJobAsync(captured, profile, progress, ct), ct)
                : RunJobAsync(captured, profile, progress, ct);

            tasks.Add(t.ContinueWith(_ => _semaphore.Release(), TaskScheduler.Default));
        }
        await Task.WhenAll(tasks);
    }

    private async Task RunJobAsync(
        ImportJob                 job,
        ConnectionProfile         profile,
        IProgress<ImportProgress> progress,
        CancellationToken         ct)
    {
        await using var adapter = _adapterFactory(profile.Provider);

        var result = new ImportResult
        {
            JobId       = job.Id,
            FilePath    = job.FilePath,
            TargetTable = job.TargetTable,
            StartedAt   = DateTime.UtcNow,
        };

        try
        {
            await adapter.ConnectAsync(profile, ct);

            var strategy = _strategyFactory(job);
            result.StrategyUsed = job.ResolvedStrategy;

            var errors = new List<RowError>();
            IDbTransaction? tx = job.TxMode == TransactionMode.AllOrNothing
                ? await adapter.BeginTransactionAsync(ct)
                : null;

            int rowIndex = 0;
            foreach (var batch in strategy.ReadBatches(job.FilePath, ";", System.Text.Encoding.UTF8))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    if (job.TxMode == TransactionMode.Batch)
                        tx = await adapter.BeginTransactionAsync(ct);

                    await adapter.ExecuteBatchAsync(batch, job.Mappings, job.TargetTable, tx!, ct);
                    result.RowsCommitted += batch.Count;

                    if (job.TxMode == TransactionMode.Batch)
                        await adapter.CommitAsync(tx!);
                }
                catch (Exception ex)
                {
                    if (job.OnError == ErrorBehavior.Abort)
                    {
                        if (tx is not null) await adapter.RollbackAsync(tx);
                        throw;
                    }
                    result.RowsSkipped += batch.Count;
                    if (job.OnError == ErrorBehavior.Collect)
                        errors.Add(new RowError { RowIndex = rowIndex, Message = ex.Message });
                }
                rowIndex += batch.Count;
                result.RowsTotal = rowIndex;

                progress.Report(new ImportProgress
                {
                    JobId      = job.Id,
                    RowsDone   = result.RowsCommitted,
                    RowsTotal  = job.RowsTotal > 0 ? job.RowsTotal : rowIndex,
                    RowsFailed = result.RowsFailed,
                    Message    = $"Processed {result.RowsCommitted} rows",
                });
            }

            if (job.TxMode == TransactionMode.AllOrNothing && tx is not null)
                await adapter.CommitAsync(tx);

            result.Errors  = errors;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success   = false;
            job.ErrorMessage = ex.Message;
        }
        finally
        {
            result.FinishedAt = DateTime.UtcNow;
            progress.Report(new ImportProgress
            {
                JobId      = job.Id,
                RowsDone   = result.RowsCommitted,
                RowsTotal  = result.RowsTotal,
                RowsFailed = result.RowsFailed,
                Message    = result.Success ? "Done" : $"Failed: {job.ErrorMessage}",
                IsComplete = true,
            });
        }
    }
}
