namespace Etherna.ExecContext.AsyncLocal
{
    /// <summary>
    ///     The handler for an <see cref="AsyncLocalContext"/> initialization.
    ///     Dispose this for release the context.
    /// </summary>
    public sealed class AsyncLocalContextHandler : IAsyncLocalContextHandler
    {
        // Constructors.
        internal AsyncLocalContextHandler(IHandledAsyncLocalContext handledContext)
        {
            HandledContext = handledContext;
        }

        // Properties.
        internal IHandledAsyncLocalContext HandledContext { get; }

        // Methods.
        public void Dispose() => HandledContext.OnDisposed(this);
    }
}
