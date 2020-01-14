namespace Digicando.ExecContext.AsyncLocal
{
    /// <summary>
    ///     Interface used by <see cref="AsyncLocalContextHandler"/> for comunicate with its
    ///     creator <see cref="AsyncLocalContext"/>.
    /// </summary>
    internal interface IHandledAsyncLocalContext
    {
        void OnDisposed(IAsyncLocalContextHandler context);
    }
}
