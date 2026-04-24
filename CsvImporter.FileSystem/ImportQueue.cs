using CsvImporter.Core.Models;

namespace CsvImporter.FileSystem;

public class ImportQueue
{
    private readonly List<ImportJob> _jobs = new();
    private readonly object          _lock = new();
    private int                      _nextId = 1;

    public void Enqueue(ImportJob job)
    {
        lock (_lock)
        {
            job.Id = _nextId++;
            job.EnqueuedAt = DateTime.UtcNow;
            job.Status = JobStatus.Pending;
            _jobs.Add(job);
        }
    }

    public void EnqueueMany(IEnumerable<ImportJob> jobs)
    {
        foreach (var j in jobs) Enqueue(j);
    }

    public ImportJob? PeekNext()
    {
        lock (_lock)
            return _jobs.Where(j => j.Status == JobStatus.Pending)
                        .OrderBy(j => j.Priority)
                        .ThenBy(j => j.EnqueuedAt)
                        .FirstOrDefault();
    }

    public ImportJob? DequeueNext()
    {
        lock (_lock)
        {
            var job = _jobs.Where(j => j.Status == JobStatus.Pending)
                           .OrderBy(j => j.Priority)
                           .ThenBy(j => j.EnqueuedAt)
                           .FirstOrDefault();
            if (job is null) return null;
            job.Status    = JobStatus.Running;
            job.StartedAt = DateTime.UtcNow;
            return job;
        }
    }

    public void SetPriority(int jobId, int priority)
    {
        lock (_lock)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == jobId);
            if (job?.Status == JobStatus.Pending)
                job.Priority = priority;
        }
    }

    public void SetStatus(int jobId, JobStatus status)
    {
        lock (_lock)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == jobId);
            if (job is null) return;
            job.Status = status;
            if (status is JobStatus.Done or JobStatus.Failed or JobStatus.Skipped)
                job.FinishedAt = DateTime.UtcNow;
        }
    }

    public void UpdateProgress(int jobId, int done, int failed, int total)
    {
        lock (_lock)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == jobId);
            if (job is null) return;
            job.RowsDone   = done;
            job.RowsFailed = failed;
            job.RowsTotal  = total;
        }
    }

    public void Retry(int jobId)
    {
        lock (_lock)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == jobId && j.Status == JobStatus.Failed);
            if (job is null) return;
            job.Status      = JobStatus.Pending;
            job.ErrorMessage = null;
            job.RowsDone    = 0;
            job.RowsFailed  = 0;
            job.StartedAt   = null;
            job.FinishedAt  = null;
        }
    }

    public void Cleanup()
    {
        lock (_lock)
            _jobs.RemoveAll(j => j.Status is JobStatus.Done or JobStatus.Failed or JobStatus.Skipped);
    }

    public List<ImportJob> GetSnapshot()
    {
        lock (_lock)
            return _jobs.ToList();
    }

    public int Count
    {
        get { lock (_lock) return _jobs.Count; }
    }
}
