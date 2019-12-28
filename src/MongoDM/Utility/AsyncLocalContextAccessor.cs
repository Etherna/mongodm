using System;
using System.Threading;

namespace Digicando.MongoDM.Utility
{
    public class AsyncLocalContextAccessor : IAsyncLocalContextAccessor
    {
        // Fields.
        private static readonly AsyncLocal<AsyncLocalContext> localContextCurrent = new AsyncLocal<AsyncLocalContext>();

        // Properties.
        public AsyncLocalContext Context => localContextCurrent.Value;

        // Static properties.
        public static IAsyncLocalContextAccessor Instance { get; } = new AsyncLocalContextAccessor();

        // Methods.
        public IAsyncLocalContext GetNewLocalContext() => new AsyncLocalContext(this);

        public void OnCreated(AsyncLocalContext context)
        {
            if (localContextCurrent.Value != null)
                throw new InvalidOperationException("Only one context at time is supported");

            localContextCurrent.Value = context;
        }

        public void OnDisposed(AsyncLocalContext context) =>
            localContextCurrent.Value = null;
    }
}
