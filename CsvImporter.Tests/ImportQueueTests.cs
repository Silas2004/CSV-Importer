using CsvImporter.Core.Models;
using CsvImporter.FileSystem;

namespace CsvImporter.Tests;

public class ImportQueueTests
{
    private static ImportJob Job(int priority = 2, string file = "test.csv") => new()
    {
        FilePath  = file,
        Priority  = priority,
    };

    [Fact]
    public void DequeueNext_ReturnsHighestPriorityFirst()
    {
        var queue = new ImportQueue();
        queue.Enqueue(Job(priority: 3, file: "c.csv"));
        queue.Enqueue(Job(priority: 1, file: "a.csv"));
        queue.Enqueue(Job(priority: 2, file: "b.csv"));

        var first = queue.DequeueNext();
        Assert.Equal("a.csv", first?.FileName);
    }

    [Fact]
    public void DequeueNext_SamePriority_FIFOOrder()
    {
        var queue = new ImportQueue();
        queue.Enqueue(Job(priority: 1, file: "first.csv"));
        queue.Enqueue(Job(priority: 1, file: "second.csv"));

        var first = queue.DequeueNext();
        Assert.Equal("first.csv", first?.FileName);
    }

    [Fact]
    public void DequeueNext_SetsStatusToRunning()
    {
        var queue = new ImportQueue();
        queue.Enqueue(Job());
        var job = queue.DequeueNext()!;
        Assert.Equal(JobStatus.Running, job.Status);
        Assert.NotNull(job.StartedAt);
    }

    [Fact]
    public void PeekNext_DoesNotMutateStatus()
    {
        var queue = new ImportQueue();
        queue.Enqueue(Job());
        var peeked = queue.PeekNext();
        Assert.Equal(JobStatus.Pending, peeked?.Status);
    }

    [Fact]
    public void Retry_ResetsFailed_ToPending()
    {
        var queue = new ImportQueue();
        queue.Enqueue(Job());
        var job = queue.DequeueNext()!;
        queue.SetStatus(job.Id, JobStatus.Failed);
        queue.Retry(job.Id);
        Assert.Equal(JobStatus.Pending, job.Status);
        Assert.Null(job.ErrorMessage);
    }

    [Fact]
    public void Cleanup_RemovesDoneAndFailed()
    {
        var queue = new ImportQueue();
        // done.csv gets priority 1 (higher), so it is dequeued first
        queue.Enqueue(Job(priority: 1, file: "done.csv"));
        queue.Enqueue(Job(priority: 2, file: "keep.csv"));
        var done = queue.DequeueNext()!;    // returns "done.csv"
        queue.SetStatus(done.Id, JobStatus.Done);
        queue.Cleanup();

        var snapshot = queue.GetSnapshot();
        Assert.DoesNotContain(snapshot, j => j.FileName == "done.csv");
        Assert.Contains(snapshot, j => j.FileName == "keep.csv");
    }

    [Fact]
    public void GetSnapshot_ReturnsCopy()
    {
        var queue = new ImportQueue();
        queue.Enqueue(Job());
        var snap1 = queue.GetSnapshot();
        queue.Enqueue(Job(file: "extra.csv"));
        var snap2 = queue.GetSnapshot();
        Assert.NotEqual(snap1.Count, snap2.Count);
    }

    [Fact]
    public void SetPriority_OnlyWorksForPending()
    {
        var queue = new ImportQueue();
        queue.Enqueue(Job(priority: 2));
        var job = queue.DequeueNext()!; // now Running
        queue.SetPriority(job.Id, 1);
        Assert.Equal(2, job.Priority); // unchanged
    }
}
