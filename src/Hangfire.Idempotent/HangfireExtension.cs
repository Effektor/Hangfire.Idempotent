using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Idempotent
{
    public static class HangfireExtension
    {
        
        public static IGlobalConfiguration UseIdempotent(this IGlobalConfiguration configuration, IdempotentConfiguration? idempotentConfiguration = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            IdempotentJobAttribute.DefaultConfiguration = idempotentConfiguration ?? new IdempotentConfiguration();
            GlobalJobFilters.Filters.Add(new IdempotentJobAttribute());

            return configuration;
        }
    }
}
