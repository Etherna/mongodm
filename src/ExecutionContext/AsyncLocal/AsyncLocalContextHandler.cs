namespace Digicando.ExecContext.AsyncLocal
{
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
