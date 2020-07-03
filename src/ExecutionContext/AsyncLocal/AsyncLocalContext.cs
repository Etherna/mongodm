using System;
using System.Collections.Generic;
using System.Threading;

namespace Etherna.ExecContext.AsyncLocal
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
        private static readonly AsyncLocal<IDictionary<object, object>?> asyncLocalContext = new AsyncLocal<IDictionary<object, object>?>();

        // Properties.
        public IDictionary<object, object>? Items => asyncLocalContext.Value;

        // Static properties.
        public static IAsyncLocalContext Instance { get; } = new AsyncLocalContext();

        // Methods.
        public IAsyncLocalContextHandler InitAsyncLocalContext()
        {
            if (asyncLocalContext.Value != null)
                throw new InvalidOperationException("Only one context at time is supported");

            asyncLocalContext.Value = new Dictionary<object, object>();

            return new AsyncLocalContextHandler(this);
        }

        public void OnDisposed(IAsyncLocalContextHandler context) =>
            asyncLocalContext.Value = null;
    }
}
