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

using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Exceptions;
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Serialization.Mapping;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Repositories
{
    public class CollectionRepository<TModel, TKey> :
        RepositoryBase<TModel, TKey>,
        ICollectionRepository<TModel, TKey>
        where TModel : class, IEntityModel<TKey>
    {
        // Fields.
        private readonly CollectionRepositoryOptions<TModel> options;
        private IMongoCollection<TModel> _collection = default!;

        // Constructors.
        public CollectionRepository(string name)
            : this(new CollectionRepositoryOptions<TModel>(name))
        { }

        public CollectionRepository(CollectionRepositoryOptions<TModel> options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        // Properties.
        public IMongoCollection<TModel> Collection => _collection ??= DbContext.Database.GetCollection<TModel>(options.Name);
        public override string Name => options.Name;

        // Public methods.
        public override async Task BuildIndexesAsync(ISchemaRegister schemaRegister, CancellationToken cancellationToken = default)
        {
            var newIndexes = new List<(string name, CreateIndexModel<TModel> createIndex)>();

            // Define new indexes.
            //repository defined
            newIndexes.AddRange(options.IndexBuilders.Select(pair =>
            {
                var (keys, options) = pair;
                if (options.Name == null)
                {
                    try
                    {
                        var renderedKeys = keys.Render(Collection.DocumentSerializer, Collection.Settings.SerializerRegistry);
                        options.Name = $"doc_{ string.Join("_", renderedKeys.Names) }";
                    }
                    catch (InvalidOperationException)
                    {
                        throw new MongodmIndexBuildingException($"Can't build custom index in collection \"{Name}\"");
                    }
                }

                return (options.Name, new CreateIndexModel<TModel>(keys, options));
            }));

            //referenced documents
            var dependencies = DbContext.SchemaRegister.GetIdMemberDependenciesFromRootModel(typeof(TModel));

            var idPaths = dependencies
                .Select(dependency => dependency.MemberPathToString())
                .Distinct();

            newIndexes.AddRange(idPaths.Select(path =>
                ($"ref_{path}",
                 new CreateIndexModel<TModel>(
                    Builders<TModel>.IndexKeys.Ascending(path),
                    new CreateIndexOptions<TModel>
                    {
                        Name = $"ref_{path}",
                        Sparse = true
                    }))));

            // Get current indexes.
            var currentIndexes = new List<BsonDocument>();
            using (var indexList = await Collection.Indexes.ListAsync(cancellationToken).ConfigureAwait(false))
                while (indexList.MoveNext(cancellationToken))
                    currentIndexes.AddRange(indexList.Current);

            // Remove old indexes.
            foreach (var oldIndex in from index in currentIndexes
                                     let indexName = index.GetElement("name").Value.ToString()
                                     where indexName != "_id_"
                                     where !newIndexes.Any(newIndex => newIndex.name == indexName)
                                     select index)
            {
                await Collection.Indexes.DropOneAsync(oldIndex.GetElement("name").Value.ToString(), cancellationToken).ConfigureAwait(false);
            }

            // Build new indexes.
            if (newIndexes.Any())
                await Collection.Indexes.CreateManyAsync(newIndexes.Select(i => i.createIndex), cancellationToken).ConfigureAwait(false);
        }

        public virtual Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(
            FilterDefinition<TModel> filter,
            FindOptions<TModel, TProjection>? options = null,
            CancellationToken cancellationToken = default) =>
            Collection.FindAsync(filter, options, cancellationToken);

        public Task<TModel> FindOneAsync(
            Expression<Func<TModel, bool>> predicate,
            CancellationToken cancellationToken = default) =>
            FindOneOnDBAsync(predicate, cancellationToken);

        public virtual Task<TResult> QueryElementsAsync<TResult>(
            Func<IMongoQueryable<TModel>, Task<TResult>> query,
            AggregateOptions? aggregateOptions = null)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query));

            return query(Collection.AsQueryable(aggregateOptions));
        }

        public async Task<PaginatedEnumerable<TResult>> QueryPaginatedElementsAsync<TResult, TResultKey>(
            Func<IMongoQueryable<TModel>, IMongoQueryable<TResult>> filter,
            Expression<Func<TResult, TResultKey>> orderKeySelector,
            int page,
            int take,
            CancellationToken cancellationToken = default)
        {
            var elements = await QueryElementsAsync(elements => filter(elements)
                .Paginate(orderKeySelector, page, take)
                .ToListAsync(cancellationToken)).ConfigureAwait(false);

            var maxPage = (await QueryElementsAsync(elements => filter(elements)
                .CountAsync(cancellationToken)).ConfigureAwait(false) - 1) / take;

            return new PaginatedEnumerable<TResult>(
                elements,
                page,
                take,
                maxPage);
        }

        public virtual Task ReplaceAsync(
            object model,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default) =>
            ReplaceAsync((TModel)model, updateDependentDocuments, cancellationToken);

        public virtual Task ReplaceAsync(
            object model,
            IClientSessionHandle session,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default) =>
            ReplaceAsync((TModel)model, session, updateDependentDocuments, cancellationToken);

        public virtual Task ReplaceAsync(
            TModel model,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default) =>
            ReplaceHelperAsync(model, null, updateDependentDocuments, cancellationToken);

        public virtual Task ReplaceAsync(
            TModel model,
            IClientSessionHandle session,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default) =>
            ReplaceHelperAsync(model, session, updateDependentDocuments, cancellationToken);

        public async Task<TModel?> TryFindOneAsync(Expression<Func<TModel, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            try
            {
                return await FindOneAsync(predicate, cancellationToken).ConfigureAwait(false);
            }
            catch (MongodmEntityNotFoundException)
            {
                return null;
            }
        }

        // Protected methods.
        protected override Task CreateOnDBAsync(IEnumerable<TModel> models, CancellationToken cancellationToken) =>
            Collection.InsertManyAsync(models, null, cancellationToken);

        protected override Task CreateOnDBAsync(TModel model, CancellationToken cancellationToken) =>
            Collection.InsertOneAsync(model, null, cancellationToken);

        protected override Task DeleteOnDBAsync(TModel model, CancellationToken cancellationToken)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            return Collection.DeleteOneAsync(
                Builders<TModel>.Filter.Eq(m => m.Id, model.Id),
                cancellationToken);
        }

        protected override async Task<TModel> FindOneOnDBAsync(TKey id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            try
            {
                return await FindOneOnDBAsync(m => m.Id!.Equals(id), cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (MongodmEntityNotFoundException)
            {
                throw new MongodmEntityNotFoundException($"Can't find key {id}");
            }
        }

        // Helpers.
        private async Task<TModel> FindOneOnDBAsync(
            Expression<Func<TModel, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            using var cursor = await Collection.FindAsync(predicate, cancellationToken: cancellationToken).ConfigureAwait(false);
            var element = await cursor.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (element == default(TModel))
                throw new MongodmEntityNotFoundException("Can't find element");

            return element;
        }

        private async Task ReplaceHelperAsync(
            TModel model,
            IClientSessionHandle? session,
            bool updateDependentDocuments,
            CancellationToken cancellationToken)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            // Replace on db.
            if (session == null)
            {
                await Collection.ReplaceOneAsync(
                    Builders<TModel>.Filter.Eq(m => m.Id, model.Id),
                    model,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await Collection.ReplaceOneAsync(
                    session,
                    Builders<TModel>.Filter.Eq(m => m.Id, model.Id),
                    model,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // Update dependent documents.
            if (updateDependentDocuments)
                DbContext.DbMaintainer.OnUpdatedModel((IAuditable)model, model.Id);

            // Reset changed members.
            ((IAuditable)model).ResetChangedMembers();
        }
    }
}