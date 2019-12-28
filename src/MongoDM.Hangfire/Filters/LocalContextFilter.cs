using Digicando.MongoDM.Utility;
using Hangfire.Server;
using System.Collections.Generic;

namespace Digicando.MongoDM.HF.Filters
{
    class LocalContextFilter : IServerFilter
    {
        // Fields.
        private readonly IAsyncLocalContextAccessor localContextAccessor;
        private readonly Dictionary<string, IAsyncLocalContext> contexts = new Dictionary<string, IAsyncLocalContext>();

        // Constructors.
        public LocalContextFilter(IAsyncLocalContextAccessor localContextAccessor)
        {
            this.localContextAccessor = localContextAccessor;
        }

        // Properties.
        public void OnPerforming(PerformingContext filterContext)
        {
            lock (contexts)
            {
                contexts.Add(filterContext.BackgroundJob.Id, localContextAccessor.GetNewLocalContext());
            }
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            lock (contexts)
            {
                var jobId = filterContext.BackgroundJob.Id;
                var context = contexts[jobId];
                contexts.Remove(jobId);
                context.Dispose();
            }
        }
    }
}
