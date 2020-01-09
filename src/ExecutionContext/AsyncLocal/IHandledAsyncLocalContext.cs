namespace Digicando.ExecContext.AsyncLocal
{
    internal interface IHandledAsyncLocalContext
    {
        void OnCreated(IAsyncLocalContextHandler context);

        void OnDisposed(IAsyncLocalContextHandler context);
    }
}
