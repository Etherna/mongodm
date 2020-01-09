using Digicando.ExecContext.AsyncLocal;
using Hangfire.Server;
using System.Collections.Generic;

namespace Digicando.MongODM.HF.Filters
{
    public class AsyncLocalContextHangfireFilter : IServerFilter
    {
        // Fields.
        private readonly Dictionary<string, IAsyncLocalContextHandler> contextHandlers = new Dictionary<string, IAsyncLocalContextHandler>();
        private readonly IAsyncLocalContext asyncLocalContext;

        // Constructors.
        public AsyncLocalContextHangfireFilter(IAsyncLocalContext asyncLocalContext)
        {
            this.asyncLocalContext = asyncLocalContext;
        }

        // Properties.
        public void OnPerforming(PerformingContext filterContext)
        {
            lock (contextHandlers)
            {
                contextHandlers.Add(filterContext.BackgroundJob.Id, asyncLocalContext.StartNewAsyncLocalContext());
            }
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            lock (contextHandlers)
            {
                var jobId = filterContext.BackgroundJob.Id;
                var context = contextHandlers[jobId];
                contextHandlers.Remove(jobId);
                context.Dispose();
            }
        }
    }
}
