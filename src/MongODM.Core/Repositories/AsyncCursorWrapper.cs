// Copyright 2020-present Etherna SA
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Etherna.MongoDB.Driver;
using Etherna.MongODM.Core.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Repositories
{
    internal class AsyncCursorWrapper<TProjection> : IAsyncCursor<TProjection>
    {
        // Fields.
        private readonly IAsyncCursor<TProjection> cursor;
        private readonly DbExecutionContextHandler dbExecutionContextHandler;
        private bool disposed;

        // Constructor and dispose.
        public AsyncCursorWrapper(
            IAsyncCursor<TProjection> cursor,
            DbExecutionContextHandler dbExecutionContextHandler)
        {
            this.cursor = cursor;
            this.dbExecutionContextHandler = dbExecutionContextHandler;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            // Dispose managed resources.
            if (disposing)
            {
                cursor.Dispose();
                dbExecutionContextHandler.Dispose();
            }

            disposed = true;
        }

        // Properties.
        public IEnumerable<TProjection> Current => cursor.Current;

        // Methods.
        public bool MoveNext(CancellationToken cancellationToken = default) => cursor.MoveNext(cancellationToken);

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default) => cursor.MoveNextAsync(cancellationToken);
    }
}
