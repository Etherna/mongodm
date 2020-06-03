using Etherna.MongODM.Models;
using Etherna.MongODM.Serialization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Repositories
{
    public interface IRepository : IDbContextInitializable
    {
        IDbContext DbContext { get; }
        Type GetKeyType { get; }
        Type GetModelType { get; }

        Task BuildIndexesAsync(
            IDocumentSchemaRegister schemaRegister,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            IEntityModel model,
            CancellationToken cancellationToken = default);
    }

    public interface IRepository<TModel, TKey> : IRepository
        where TModel : class, IEntityModel<TKey>
    {
        Task CreateAsync(
            TModel model,
            CancellationToken cancellationToken = default);

        Task CreateAsync(
            IEnumerable<TModel> models,
            CancellationToken cancellationToken = default);

        Task<TModel> FindOneAsync(
            TKey id,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            TModel model,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            TKey id,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Try to find a model and don't throw exception if it is not found
        /// </summary>
        /// <param name="id">Model's Id</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The model, null if it doesn't exist</returns>
        Task<TModel?> TryFindOneAsync(
            TKey id,
            CancellationToken cancellationToken = default);
    }
}