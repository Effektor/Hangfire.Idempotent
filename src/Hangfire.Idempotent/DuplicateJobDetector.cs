using Hangfire.Common;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Idempotent;

public class DuplicateJobDetector
{
    private readonly IMonitoringApi monitoringApi;
    private readonly object lockObject = new();
    private readonly IdempotentConfiguration configuration;
    private readonly string defaultQueue;

    public DuplicateJobDetector(IMonitoringApi monitoringApi, IdempotentConfiguration configuration, string defaultQueue = "default")
    {
        this.monitoringApi = monitoringApi ?? throw new ArgumentNullException(nameof(monitoringApi));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.defaultQueue = defaultQueue ?? "default";
    }

    public bool IsDuplicateJob(Job targetJob)
    {
        List<KeyValuePair<string, EnqueuedJobDto?>> enqueued;
        List<KeyValuePair<string, ScheduledJobDto?>> scheduled;
        List<KeyValuePair<string, FetchedJobDto?>> fetched;

        // Todo: See if we can can insert ourselves into the state change pipeline so we can lock around state changes and avoid race conditions.
        lock (lockObject)
        {
            enqueued = monitoringApi.EnqueuedJobs(targetJob.Queue ?? defaultQueue, 0, configuration.MaxRetrievals).ToList();
            scheduled = monitoringApi.ScheduledJobs(0, configuration.MaxRetrievals).ToList();
            fetched = monitoringApi.FetchedJobs(targetJob.Queue ?? defaultQueue, 0, configuration.MaxRetrievals).ToList();
        }

        foreach (var pair in enqueued)
        {
            if (pair.Value?.Job != null && AreJobsEqual(pair.Value.Job, targetJob))
                return true;
        }

        foreach (var pair in scheduled)
        {
            if (pair.Value?.Job != null && AreJobsEqual(pair.Value.Job, targetJob))
                return true;
        }

        foreach (var pair in fetched)
        {
            if (pair.Value?.Job != null && AreJobsEqual(pair.Value.Job, targetJob))
                return true;
        }

        return false;
    }

    private static bool AreJobsEqual(Job a, Job b)
    {
        if (a.Type != b.Type || a.Method.Name != b.Method.Name) return false;
        if (a.Args.Count != b.Args.Count) return false;

        for (int i = 0; i < a.Args.Count; i++)
        {
            if (!Equals(a.Args[i], b.Args[i])) return false;
        }

        return true;
    }
}
