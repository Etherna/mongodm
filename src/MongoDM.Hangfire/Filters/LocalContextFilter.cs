using Digicando.MongoDM.Utility;
using Hangfire.Server;
using System.Collections.Generic;

namespace Digicando.MongoDM.HF.Filters
{
    class LocalContextFilter : IServerFilter
    {
        // Fields.
        private readonly IAsyncLocalContextAccessor localContextAccessor;
        private readonly Dictionary<PerformContext, IAsyncLocalContext> contexts = new Dictionary<PerformContext, IAsyncLocalContext>();

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
                contexts.Add(filterContext, localContextAccessor.GetNewLocalContext());
            }
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            lock (contexts)
            {
                var context = contexts[filterContext];
                contexts.Remove(filterContext);
                context.Dispose();
            }
        }
    }
}
