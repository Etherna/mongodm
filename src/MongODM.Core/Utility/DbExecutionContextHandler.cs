// Copyright 2020-present Etherna SA
// This file is part of MongODM.
// 
// MongODM is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// MongODM is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with MongODM.
// If not, see <https://www.gnu.org/licenses/>.

using Etherna.ExecContext;
using Etherna.ExecContext.AsyncLocal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Utility
{
    public sealed class DbExecutionContextHandler : IDisposable
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

            asyncLocalContextHandler?.Dispose();
        }

        // Properties.
        public IDbContext DbContext { get; }

        // Static methods.
        public static IDbContext? TryGetCurrentDbContext(IExecutionContext executionContext)
        {
            ArgumentNullException.ThrowIfNull(executionContext, nameof(executionContext));

            if (executionContext.Items is null ||
                !executionContext.Items.ContainsKey(HandlerKey))
                return null;

            var requestes = (ICollection<DbExecutionContextHandler>)executionContext.Items[HandlerKey]!;

            //get the last with a stack system, for recursing calls between different dbContexts
            lock (((ICollection)requestes).SyncRoot)
                return requestes.Reverse().FirstOrDefault()?.DbContext;
        }
    }
}
