using System.Collections.Generic;

namespace Digicando.MongoDM.Utility
{
    public class AsyncLocalContext : IAsyncLocalContext
    {
        // Fields.
        private readonly IAsyncLocalContextAccessor localContextAccessor;

        // Constructors.
        internal AsyncLocalContext(IAsyncLocalContextAccessor localContextAccessor)
        {
            this.localContextAccessor = localContextAccessor;
            localContextAccessor.OnCreated(this);
        }

        // Properties.
        public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();

        public void Dispose() =>
            localContextAccessor.OnDisposed(this);
    }
}
