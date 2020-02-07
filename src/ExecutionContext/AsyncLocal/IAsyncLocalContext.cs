namespace Digicando.ExecContext.AsyncLocal
{
    /// <summary>
    ///     The <see cref="AsyncLocalContext"/> interface.
    ///     Permits to create an async local context living with the method calling tree.
    /// </summary>
    public interface IAsyncLocalContext : IExecutionContext
    {
        IAsyncLocalContextHandler InitAsyncLocalContext();
    }
}