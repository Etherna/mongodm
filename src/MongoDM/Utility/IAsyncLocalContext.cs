using System;
using System.Collections.Generic;

namespace Digicando.MongoDM.Utility
{
    public interface IAsyncLocalContext : IDisposable
    {
        // Properties.
        IDictionary<string, object> Items { get; }
    }
}
