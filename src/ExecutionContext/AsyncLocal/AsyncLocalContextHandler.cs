namespace Digicando.ExecContext.AsyncLocal
{
    /// <summary>
    ///     The handler for an <see cref="AsyncLocalContext"/> initialization.
    ///     Dispose this for release the context.
    /// </summary>
    public class AsyncLocalContextHandler : IAsyncLocalContextHandler
    {
        // Fields.
        private readonly IHandledAsyncLocalContext handledContext;

        // Constructors.
        internal AsyncLocalContextHandler(IHandledAsyncLocalContext handledContext)
        {
            this.handledContext = handledContext;
            handledContext.OnCreated(this);
        }

        // Methods.
        public void Dispose() =>
            handledContext.OnDisposed(this);
    }
}
