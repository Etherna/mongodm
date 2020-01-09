using System;
using System.Collections.Generic;
using System.Threading;

namespace Digicando.ExecContext.AsyncLocal
{
    public class AsyncLocalContext : IAsyncLocalContext, IHandledAsyncLocalContext
    {
        // Fields.
        private static readonly AsyncLocal<IDictionary<string, object>> asyncLocalContext = new AsyncLocal<IDictionary<string, object>>();

        // Properties.
        public IDictionary<string, object> Items => asyncLocalContext.Value;

        // Methods.
        public IAsyncLocalContextHandler StartNewAsyncLocalContext() => new AsyncLocalContextHandler(this);

        public void OnCreated(IAsyncLocalContextHandler context)
        {
            if (asyncLocalContext.Value != null)
                throw new InvalidOperationException("Only one context at time is supported");

            asyncLocalContext.Value = new Dictionary<string, object>();
        }

        public void OnDisposed(IAsyncLocalContextHandler context) =>
            asyncLocalContext.Value = null;
    }
}
