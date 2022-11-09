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

using Etherna.MongoDB.Driver;
using Etherna.MongoDB.Driver.Linq;
using Etherna.MongODM.Core.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Repositories
{
    public interface IRepository : IDbContextInitializable
    {
        IDbContext DbContext { get; }
        Type KeyType { get; }
        Type ModelType { get; }
        string Name { get; }

        Task BuildIndexesAsync(
            CancellationToken cancellationToken = default);

        Task CreateAsync(
            object model,
            CancellationToken cancellationToken = default);

        Task CreateAsync(
            IEnumerable<object> models,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            IEntityModel model,
            CancellationToken cancellationToken = default);

        Task<object> FindOneAsync(
            object id,
            CancellationToken cancellationToken = default);

        string ModelIdToString(object model);

        Task ReplaceAsync(
            object model,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default);

        Task ReplaceAsync(
            object model,
            IClientSessionHandle session,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Try to find a model and don't throw exception if it is not found
        /// </summary>
        /// <param name="id">Model's Id</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The model, null if it doesn't exist</returns>
        Task<object?> TryFindOneAsync(
            object id,
            CancellationToken cancellationToken = default);
    }

    public interface IRepository<TModel, TKey> : IRepository
        where TModel : class, IEntityModel<TKey>
    {
        Task AccessToCollectionAsync(Func<IMongoCollection<TModel>, Task> action);

        Task<TResult> AccessToCollectionAsync<TResult>(Func<IMongoCollection<TModel>, Task<TResult>> func);

        Task CreateAsync(
            TModel model,
            CancellationToken cancellationToken = default);

        Task CreateAsync(
            IEnumerable<TModel> models,
            CancellationToken cancellationToken = default);

        Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(
            FilterDefinition<TModel> filter,
            FindOptions<TModel, TProjection>? options = null,
            CancellationToken cancellationToken = default);

        Task<TModel> FindOneAsync(
            TKey id,
            CancellationToken cancellationToken = default);

        Task<TModel> FindOneAsync(
            Expression<Func<TModel, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            TModel model,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            TKey id,
            CancellationToken cancellationToken = default);

        Task<TResult> QueryElementsAsync<TResult>(
            Func<IMongoQueryable<TModel>, Task<TResult>> query,
            AggregateOptions? aggregateOptions = null);

        Task<PaginatedEnumerable<TResult>> QueryPaginatedElementsAsync<TResult, TResultKey>(
            Func<IMongoQueryable<TModel>, IMongoQueryable<TResult>> filter,
            Expression<Func<TResult, TResultKey>> orderKeySelector,
            int page,
            int take,
            bool useDescendingOrder = false,
            CancellationToken cancellationToken = default);

        Task ReplaceAsync(
            TModel model,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default);

        Task ReplaceAsync(
            TModel model,
            IClientSessionHandle session,
            bool updateDependentDocuments = true,
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

        /// <summary>
        /// Try to find a model and don't throw exception if it is not found
        /// </summary>
        /// <param name="predicate">Model find predicate</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The model, null if it doesn't exist</returns>
        Task<TModel?> TryFindOneAsync(
            Expression<Func<TModel, bool>> predicate,
            CancellationToken cancellationToken = default);
    }
}