namespace Digicando.ExecContext.AsyncLocal
{
    /// <summary>
    ///     Interface used by <see cref="AsyncLocalContextHandler"/> for comunicate with its
    ///     creator <see cref="AsyncLocalContext"/>.
    /// </summary>
    internal interface IHandledAsyncLocalContext
    {
        void OnCreated(IAsyncLocalContextHandler context);

        void OnDisposed(IAsyncLocalContextHandler context);
    }
}
