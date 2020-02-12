using Digicando.MongODM.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongODM.Repositories
{
    public interface ICollectionRepository : IRepository
    {
        Task ReplaceAsync(
            object model,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default);

        Task ReplaceAsync(
            object model,
            IClientSessionHandle session,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default);
    }

    public interface ICollectionRepository<TModel, TKey> : IRepository<TModel, TKey>, ICollectionRepository
        where TModel : class, IEntityModel<TKey>
    {
        IMongoCollection<TModel> Collection { get; }

        Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(
            FilterDefinition<TModel> filter,
            FindOptions<TModel, TProjection> options = null,
            CancellationToken cancellationToken = default);

        Task<TResult> QueryElementsAsync<TResult>(
            Func<IMongoQueryable<TModel>, Task<TResult>> query,
            AggregateOptions aggregateOptions = null);

        Task ReplaceAsync(
            TModel model,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default);

        Task ReplaceAsync(
            TModel model,
            IClientSessionHandle session,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default);
    }
}
