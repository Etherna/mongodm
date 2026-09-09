// Copyright 2020-present Etherna SA
// This file is part of Scrinium.
//
// Scrinium is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
//
// Scrinium is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along with Scrinium.
// If not, see <https://www.gnu.org/licenses/>.

using Etherna.MongoDB.Driver;
using Etherna.Scrinium.Core.ExecContext.AsyncLocal;
using Etherna.Scrinium.Core.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.Scrinium.Core.Utility
{
    /// <summary>
    /// Associates a database session to the current execution context flow, for the scope
    /// of the handler. While the handler is active, operations invoked without an explicit
    /// session on collections of the same engine enlist automatically in the handled
    /// session, joining its transaction when one is active.
    /// </summary>
    /// <remarks>
    /// Database sessions don't support concurrent operations: keep operations sequential
    /// inside the handler scope.
    /// </remarks>
    public sealed class DbSessionHandler : IDisposable
    {
        // Consts.
        private const string HandlerKey = "DbSessionHandler";

        // Fields.
        private readonly IAsyncLocalContextHandler? asyncLocalContextHandler;
        private readonly List<Action> commitActions = [];
        private readonly ICollection<DbSessionHandler> requests;

        // Constructors and dispose.
        public DbSessionHandler(
            IDbContextEngine dbContextEngine,
            IClientSessionHandle session)
        {
            DbContextEngine = dbContextEngine ?? throw new ArgumentNullException(nameof(dbContextEngine));
            Session = session ?? throw new ArgumentNullException(nameof(session));

            var executionContext = dbContextEngine.ExecutionContext;

            if (executionContext.Items is null) //if an execution context doesn't exist, create it
                asyncLocalContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();

            requests = executionContext.GetOrAddItemsList<DbSessionHandler>(HandlerKey);

            lock (((ICollection)requests).SyncRoot)
                requests.Add(this);
        }

        public void Dispose()
        {
            lock (((ICollection)requests).SyncRoot)
                requests.Remove(this);

            asyncLocalContextHandler?.Dispose();
        }

        // Properties.
        public IDbContextEngine DbContextEngine { get; }
        public IClientSessionHandle Session { get; }

        // Static methods.
        public static IClientSessionHandle? TryGetCurrentSession(IDbContextEngine dbContextEngine) =>
            TryGetCurrentHandler(dbContextEngine)?.Session;

        // Internals.
        /// <summary>
        /// Run the bookkeeping the enlisted operations deferred to the commit of the handled
        /// transaction, in registration order, once the transaction committed.
        /// </summary>
        internal void RunCommitActions()
        {
            foreach (var action in commitActions)
                action();
            commitActions.Clear();
        }

        /// <summary>
        /// Defer an operation bookkeeping to the commit of the ambient transaction of the
        /// engine. Without an ambient session handler nothing defers, and the bookkeeping
        /// stays with the caller.
        /// </summary>
        /// <param name="dbContextEngine">The engine of the enlisted operation</param>
        /// <param name="action">The bookkeeping to run at commit</param>
        /// <returns>True if the action deferred to the commit</returns>
        internal static bool TryDeferToTransactionCommit(IDbContextEngine dbContextEngine, Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (TryGetCurrentHandler(dbContextEngine) is not { } currentHandler)
                return false;

            currentHandler.commitActions.Add(action);
            return true;
        }

        // Helpers.
        private static DbSessionHandler? TryGetCurrentHandler(IDbContextEngine dbContextEngine)
        {
            ArgumentNullException.ThrowIfNull(dbContextEngine);

            var requests = dbContextEngine.ExecutionContext.TryGetItemsList<DbSessionHandler>(HandlerKey);
            if (requests is null)
                return null;

            /* Get the last handler of the same engine with a stack system, for nesting
             * sessions between different db contexts. Sessions are per connection: handlers
             * of other engines don't apply. */
            lock (((ICollection)requests).SyncRoot)
                return requests
                    .Where(handler => handler.DbContextEngine == dbContextEngine)
                    .Reverse()
                    .FirstOrDefault();
        }
    }
}
