using Etherna.ExecContext;
using Etherna.ExecContext.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Utility
{
    internal class DbContextExecutionContextHandler : IDisposable
    {
        // Consts.
        private const string HandlerKey = "DbContextExecutionContextHandler";

        // Fields.
        private readonly ICollection<DbContextExecutionContextHandler> requestes;

        // Constructors and dispose.
        public DbContextExecutionContextHandler(
            IDbContext dbContext)
        {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            var executionContext = dbContext.ExecutionContext;
            if (executionContext.Items is null)
                throw new ExecutionContextNotFoundException();

            if (!executionContext.Items.ContainsKey(HandlerKey))
                executionContext.Items.Add(HandlerKey, new List<DbContextExecutionContextHandler>());

            requestes = (ICollection<DbContextExecutionContextHandler>)executionContext.Items[HandlerKey]!;

            lock (((ICollection)requestes).SyncRoot)
                requestes.Add(this);
        }

        public void Dispose()
        {
            lock (((ICollection)requestes).SyncRoot)
                requestes.Remove(this);
        }

        // Properties.
        public IDbContext DbContext { get; }

        // Static methods.
        public static IDbContext? GetCurrentDbContext(IExecutionContext executionContext)
        {
            if (executionContext.Items is null)
                throw new ExecutionContextNotFoundException();

            if (!executionContext.Items.ContainsKey(HandlerKey))
                return null;

            var requestes = (ICollection<DbContextExecutionContextHandler>)executionContext.Items[HandlerKey]!;

            //get the last with a stack system, for recursing calls betweem different dbContexts
            lock (((ICollection)requestes).SyncRoot)
                return requestes.Reverse().FirstOrDefault()?.DbContext;
        }
    }
}
