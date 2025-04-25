using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Idempotent
{
    public class IdempotentConfiguration
    {
        const int DefaultMaxRetrievals = 500;

        public int MaxRetrievals { get; set; } = DefaultMaxRetrievals;

#pragma warning disable CS8618
        internal static IdempotentConfiguration Default {get;set;}
    


}
}
