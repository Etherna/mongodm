namespace Digicando.ExecContext.AsyncLocal
{
    public interface IAsyncLocalContext : IContextAccessor
    {
        IAsyncLocalContextHandler StartNewAsyncLocalContext();
    }
}