using System;

namespace Etherna.ExecContext.AsyncLocal
{
    /// <summary>
    ///     The <see cref="AsyncLocalContext"/> interface.
    ///     Permits to create an async local context living with the method calling tree.
    /// </summary>
    public interface IAsyncLocalContext : IExecutionContext
    {
        /// <summary>
        /// Initialize a new async local context
        /// </summary>
        /// <returns>The new context handler</returns>
        /// <exception cref="InvalidOperationException">Throw when another local context is found</exception>
        IAsyncLocalContextHandler InitAsyncLocalContext();
    }
}