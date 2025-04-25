using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Idempotent
{
    internal class IdempotentBackgroundJobClient : IBackgroundJobClient
    {
        public bool ChangeState([NotNull] string jobId, [NotNull] IState state, [CanBeNull] string expectedState)
        {
            throw new NotImplementedException();
        }

        public string Create([NotNull] Job job, [NotNull] IState state)
        {
            throw new NotImplementedException();
        }
    }
}
