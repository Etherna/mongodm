using Digicando.ExecContext.AsyncLocal;
using Hangfire.Server;
using System;
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
            if (filterContext is null)
                throw new ArgumentNullException(nameof(filterContext));

            lock (contextHandlers)
            {
                contextHandlers.Add(filterContext.BackgroundJob.Id, asyncLocalContext.InitAsyncLocalContext());
            }
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            if (filterContext is null)
                throw new ArgumentNullException(nameof(filterContext));

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
