using Etherna.ExecContext;
using Etherna.ExecContext.AsyncLocal;
using Etherna.ExecContext.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Utility
{
    internal class DbExecutionContextHandler : IDisposable
    {
        // Consts.
        private const string HandlerKey = "DbContextExecutionContextHandler";

        // Fields.
        private readonly IAsyncLocalContextHandler? asyncLocalContextHandler;
        private readonly ICollection<DbExecutionContextHandler> requestes;

        // Constructors and dispose.
        public DbExecutionContextHandler(
            IDbContext dbContext)
        {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            var executionContext = dbContext.ExecutionContext;

            if (executionContext.Items is null) //if an execution context doesn't exist, create it
                asyncLocalContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();

            if (!executionContext.Items!.ContainsKey(HandlerKey))
                executionContext.Items.Add(HandlerKey, new List<DbExecutionContextHandler>());

            requestes = (ICollection<DbExecutionContextHandler>)executionContext.Items[HandlerKey]!;

            lock (((ICollection)requestes).SyncRoot)
                requestes.Add(this);
        }

        public void Dispose()
        {
            lock (((ICollection)requestes).SyncRoot)
                requestes.Remove(this);

            if (asyncLocalContextHandler is not null)
                asyncLocalContextHandler.Dispose();
        }

        // Properties.
        public IDbContext DbContext { get; }

        // Static methods.
        public static IDbContext? TryGetCurrentDbContext(IExecutionContext executionContext)
        {
            if (executionContext.Items is null)
                throw new ExecutionContextNotFoundException();

            if (!executionContext.Items.ContainsKey(HandlerKey))
                return null;

            var requestes = (ICollection<DbExecutionContextHandler>)executionContext.Items[HandlerKey]!;

            //get the last with a stack system, for recursing calls betweem different dbContexts
            lock (((ICollection)requestes).SyncRoot)
                return requestes.Reverse().FirstOrDefault()?.DbContext;
        }
    }
}
