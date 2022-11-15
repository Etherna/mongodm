//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

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
            if (executionContext is null)
                throw new ArgumentNullException(nameof(executionContext));

            if (executionContext.Items is null ||
                !executionContext.Items.ContainsKey(HandlerKey))
                return null;

            var requestes = (ICollection<DbExecutionContextHandler>)executionContext.Items[HandlerKey]!;

            //get the last with a stack system, for recursing calls betweem different dbContexts
            lock (((ICollection)requestes).SyncRoot)
                return requestes.Reverse().FirstOrDefault()?.DbContext;
        }
    }
}
