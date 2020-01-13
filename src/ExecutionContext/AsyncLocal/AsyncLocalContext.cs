using System;
using System.Collections.Generic;
using System.Threading;

namespace Digicando.ExecContext.AsyncLocal
{
    /// <summary>
    ///     Async local context implementation. This can be used as singleton or with multiple instances.
    ///     The <see cref="AsyncLocal{T}"/> container permits to have an Item instance inside this
    ///     method calling tree.
    /// </summary>
    /// <remarks>
    ///     Before try to use an async local context, call the method <see cref="InitAsyncLocalContext"/>
    ///     for initialize the <see cref="AsyncLocal{T}"/> container, and receive a context handler.
    ///     After have used, dispose the handler for destroy the current context dictionary.
    /// </remarks>
    public class AsyncLocalContext : IAsyncLocalContext, IHandledAsyncLocalContext
    {
        // Fields.
        private static readonly AsyncLocal<IDictionary<string, object>> asyncLocalContext = new AsyncLocal<IDictionary<string, object>>();

        // Properties.
        public IDictionary<string, object> Items => asyncLocalContext.Value;

        // Methods.
        public IAsyncLocalContextHandler InitAsyncLocalContext() => new AsyncLocalContextHandler(this);

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
