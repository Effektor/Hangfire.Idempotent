using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Idempotent;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class IdempotentJobAttribute : JobFilterAttribute, IClientFilter, IApplyStateFilter
{
    public void OnCreated(CreatedContext context)
    {
        
    }

    public void OnCreating(CreatingContext context)
    {
        if (ShouldDeduplicate(context.Job))
            if(IsDuplicateJob(context.Job))
                context.Canceled = true;

    }

    private bool IsDuplicateJob(Job targetJob)
    {
        var monitor = JobStorage.Current.GetMonitoringApi();

        var state = new { Queue = "default" };

        List<KeyValuePair<string, EnqueuedJobDto>> enqueued;
        List<KeyValuePair<string, ScheduledJobDto>> scheduled;
        List<KeyValuePair<string, FetchedJobDto>> fetched;

        lock (lockObject)
        {
            enqueued = monitor.EnqueuedJobs(state.Queue ?? "default", 0, 100).ToList();
            scheduled = monitor.ScheduledJobs(0, 100).ToList();
            fetched = monitor.FetchedJobs(state.Queue ?? "default", 0, 100).ToList();
        }

        foreach (var pair in enqueued)
        {
            var existing = pair.Value.Job;
            if (AreJobsEqual(existing, targetJob)) return true;
        }
        
        foreach (var pair in scheduled)
        {
            var existing = pair.Value.Job;
            if (AreJobsEqual(existing, targetJob)) return true;
        }
        
        foreach (var pair in fetched)
        {
            var existing = pair.Value.Job;
            if (AreJobsEqual(existing, targetJob)) return true;
        }

        return false;
    }

    private bool AreJobsEqual(Job a, Job b)
    {
        if (a.Type != b.Type || a.Method.Name != b.Method.Name) return false;
        if (a.Args.Count != b.Args.Count) return false;

        for (int i = 0; i < a.Args.Count; i++)
        {
            if (!Equals(a.Args[i], b.Args[i])) return false;
        }

        return true;
    }

    private object lockObject = new object();

    private bool ShouldDeduplicate(Job job)
    {
        var method = job.Method;
        var attributes = method.GetCustomAttributes(typeof(IdempotentJobAttribute), false);
        return attributes != null;
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        lock(lockObject)
        {
            
            
            
        }
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        
    }
}
