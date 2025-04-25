using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Idempotent
{
    public static class HangfireExtension
    {
        
        public static IGlobalConfiguration UseIdempotent(this IGlobalConfiguration configuration, IdempotentOptions options = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            GlobalJobFilters.Filters.Add(new IdempotentJobAttribute());

            return configuration;
        }

        public static void EnqueIdempotent(this IBackgroundJobClient backgroundJobClient, string jobId, string jobName, object jobData)
        {
            /*
            if (backgroundJobClient == null) throw new ArgumentNullException(nameof(backgroundJobClient));
            if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException(nameof(jobId));
            if (string.IsNullOrEmpty(jobName)) throw new ArgumentNullException(nameof(jobName));
            if (jobData == null) throw new ArgumentNullException(nameof(jobData));
            
            backgroundJobClient.Enqueue(() => job.Execute());*/
        }
    }
}
