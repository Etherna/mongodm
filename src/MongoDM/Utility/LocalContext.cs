using System;
using System.Collections.Generic;

namespace Digicando.MongoDM.Utility
{
    public class LocalContext : IDisposable
    {
        // Fields.
        private readonly ILocalContextAccessor localContextAccessor;

        // Constructors.
        internal LocalContext(ILocalContextAccessor localContextAccessor)
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
