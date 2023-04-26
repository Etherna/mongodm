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
