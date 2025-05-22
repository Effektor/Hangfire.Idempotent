using Hangfire.Client;
using Hangfire.Common;

namespace Hangfire.Idempotent;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class IdempotentJobAttribute : JobFilterAttribute, IClientFilter
{
    internal static IdempotentConfiguration DefaultConfiguration { get; set; } = new IdempotentConfiguration();
    private readonly DuplicateJobDetector detector;

    public IdempotentJobAttribute()
    {
        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        detector = new DuplicateJobDetector(monitoringApi, DefaultConfiguration);
    }
    
    public void OnCreated(CreatedContext context) { }

    public void OnCreating(CreatingContext context)
    {
        if (ShouldDeduplicate(context.Job))
        {
            if (detector.IsDuplicateJob(context.Job))
                context.Canceled = true;
        }
    }

    private bool ShouldDeduplicate(Job job)
    {
        var method = job.Method;
        var attributes = method.GetCustomAttributes(typeof(IdempotentJobAttribute), false);
        return attributes != null;
    }
}
