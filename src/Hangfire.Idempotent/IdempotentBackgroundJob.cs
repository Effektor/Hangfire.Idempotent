using Hangfire.Annotations;
using Hangfire.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire;


partial class IdempotentBackgroundJob : BackgroundJob
{


    public IdempotentBackgroundJob([NotNull] string id, [CanBeNull] Job job, DateTime createdAt) : base(id, job, createdAt)
    {
    }

    internal static void Init()
    {
        
    }


}
