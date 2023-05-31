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

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Driver;
using Etherna.MongoDB.Driver.Linq;
using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Exceptions;
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Repositories
{
    public class Repository<TModel, TKey> :
        IRepository<TModel, TKey>
        where TModel : class, IEntityModel<TKey>
    {
        // Fields.
        private ILogger logger = default!;
        private readonly RepositoryOptions<TModel> options;
        private IMongoCollection<TModel> _collection = default!;

        // Constructors.
        public Repository(string name)
            : this(new RepositoryOptions<TModel>(name))
        { }

        public Repository(RepositoryOptions<TModel> options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        // Initializer.
        public virtual void Initialize(IDbContext dbContext, ILogger logger)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            IsInitialized = true;

            this.logger.RepositoryInitialized(Name, dbContext.Options.DbName);
        }

        // Properties.
        public IDbContext DbContext { get; private set; } = default!;
        public Type KeyType => typeof(TKey);
        public Type ModelType => typeof(TModel);
        public bool IsInitialized { get; private set; }
        public string Name => options.Name;

        // Public methods.
        public Task AccessToCollectionAsync(
            Func<IMongoCollection<TModel>, Task> action,
            bool handleImplicitDbExecutionContext = true) =>
            AccessToCollectionAsync(async collection =>
            {
                await action(collection).ConfigureAwait(false);
                return 0;
            }, handleImplicitDbExecutionContext);

        public async Task<TResult> AccessToCollectionAsync<TResult>(
            Func<IMongoCollection<TModel>, Task<TResult>> func,
            bool handleImplicitDbExecutionContext = true)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));

            // Initialize collection cache.
            _collection ??= DbContext.Database.GetCollection<TModel>(options.Name);

            // Invoke func into optional implicit execution context.
            DbExecutionContextHandler? dbExecContextHandler = null;
            if (handleImplicitDbExecutionContext)
                dbExecContextHandler = new DbExecutionContextHandler(DbContext);

            var result = await func(_collection).ConfigureAwait(false);

            dbExecContextHandler?.Dispose();

            logger.RepositoryAccessedCollection(Name, DbContext.Options.DbName);

            return result;
        }

        public virtual Task BuildIndexesAsync(CancellationToken cancellationToken = default) =>
            AccessToCollectionAsync(async collection =>
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
                            var renderedKeys = keys.Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry);
                            options.Name = $"doc_{string.Join("_", renderedKeys.Names)}";
                        }
                        catch (InvalidOperationException)
                        {
                            throw new MongodmIndexBuildingException($"Can't build custom index in collection \"{Name}\"");
                        }
                    }

                    return (options.Name, new CreateIndexModel<TModel>(keys, options));
                }));

                //referenced documents
                var idMemberMaps = DbContext.MapRegistry.TryGetModelMap(typeof(TModel), out IModelMap? modelMap) ?
                    modelMap!.AllDescendingMemberMaps.Where(mm => mm.IsEntityReferenceMember && mm.IsIdMember) :
                    Array.Empty<IMemberMap>();

                var idPaths = idMemberMaps
                    .Select(mm => string.Join(".", mm.MemberMapPath.Select(pathMM => pathMM.BsonMemberMap.ElementName)))
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
                using (var indexList = await collection.Indexes.ListAsync(cancellationToken).ConfigureAwait(false))
                    while (await indexList.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                        currentIndexes.AddRange(indexList.Current);

                // Remove old indexes.
                foreach (var oldIndex in from index in currentIndexes
                                         let indexName = index.GetElement("name").Value.ToString()
                                         where indexName != "_id_"
                                         where !newIndexes.Any(newIndex => newIndex.name == indexName)
                                         select index)
                {
                    await collection.Indexes.DropOneAsync(oldIndex.GetElement("name").Value.ToString(), cancellationToken).ConfigureAwait(false);
                }

                // Build new indexes.
                if (newIndexes.Any())
                    await collection.Indexes.CreateManyAsync(newIndexes.Select(i => i.createIndex), cancellationToken).ConfigureAwait(false);

                logger.RepositoryBuiltIndexes(Name, DbContext.Options.DbName);
            });

        public Task CreateAsync(object model, CancellationToken cancellationToken = default) =>
            CreateAsync((TModel)model, cancellationToken);

        public Task CreateAsync(IEnumerable<object> models, CancellationToken cancellationToken = default) =>
            CreateAsync(models.Select(m => (TModel)m), cancellationToken);

        public virtual async Task CreateAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default)
        {
            await CreateOnDBAsync(models, cancellationToken).ConfigureAwait(false);

            logger.RepositoryCreatedDocuments(Name, DbContext.Options.DbName, models.Select(m => m.Id!.ToString()));

            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task CreateAsync(TModel model, CancellationToken cancellationToken = default)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            await CreateOnDBAsync(model, cancellationToken).ConfigureAwait(false);

            logger.RepositoryCreatedDocument(Name, DbContext.Options.DbName, model.Id!.ToString());

            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var model = await FindOneAsync(id, cancellationToken: cancellationToken).ConfigureAwait(false);
            await DeleteAsync(model, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task DeleteAsync(TModel model, CancellationToken cancellationToken = default)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            // Unlink dependent models.
            model.DisposeForDelete();
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Delete model.
            await DeleteOnDBAsync(model, cancellationToken).ConfigureAwait(false);

            // Remove from cache.
            if (DbContext.DbCache.LoadedModels.ContainsKey(model.Id!))
                DbContext.DbCache.RemoveModel(model.Id!);

            logger.RepositoryDeletedDocument(Name, DbContext.Options.DbName, model.Id!.ToString());
        }

        public async Task DeleteAsync(IEntityModel model, CancellationToken cancellationToken = default)
        {
            if (model is not TModel castedModel)
                throw new MongodmInvalidEntityTypeException("Invalid model type");
            await DeleteAsync(castedModel, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(
            FilterDefinition<TModel> filter,
            FindOptions<TModel, TProjection>? options = null,
            CancellationToken cancellationToken = default)
        {
            // Create an explicit db execution context. It needs to survive until cursor is alive.
            var dbExecContextHandler = new DbExecutionContextHandler(DbContext);

            return await AccessToCollectionAsync(async collection =>
            {
                var resultCursor = await collection.FindAsync(filter, options, cancellationToken);
                var wrappedCursor = new AsyncCursorWrapper<TProjection>(resultCursor, dbExecContextHandler);

                logger.RepositoryQueriedCollection(Name, DbContext.Options.DbName);

                return wrappedCursor;
            }, false);
        }

        public async Task<object> FindOneAsync(object id, CancellationToken cancellationToken = default) =>
            await FindOneAsync((TKey)id, cancellationToken).ConfigureAwait(false);

        public virtual async Task<TModel> FindOneAsync(
            TKey id,
            CancellationToken cancellationToken = default)
        {
            if (DbContext.DbCache.LoadedModels.ContainsKey(id!))
            {
                var cachedModel = DbContext.DbCache.LoadedModels[id!] as TModel;
                if (cachedModel is IReferenceable { IsSummary: false })
                    return cachedModel!;
            }

            return await FindOneOnDBAsync(id, cancellationToken).ConfigureAwait(false);
        }

        public Task<TModel> FindOneAsync(
            Expression<Func<TModel, bool>> predicate,
            CancellationToken cancellationToken = default) =>
            FindOneOnDBAsync(predicate, cancellationToken);

        public string ModelIdToString(object model)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));
            if (model is not TModel typedModel)
                throw new ArgumentException($"Model is not of {model.GetType().Name} type", nameof(model));
            if (typedModel.Id is null)
                throw new InvalidOperationException("Model Id can't be null");

            return typedModel.Id.ToString();
        }

        public virtual Task<TResult> QueryElementsAsync<TResult>(
            Func<IMongoQueryable<TModel>, Task<TResult>> query,
            AggregateOptions? aggregateOptions = null) =>
            AccessToCollectionAsync(collection =>
            {
                if (query is null)
                    throw new ArgumentNullException(nameof(query));

                var result = query(collection.AsQueryable(aggregateOptions));

                logger.RepositoryQueriedCollection(Name, DbContext.Options.DbName);

                return result;
            });

        public async Task<PaginatedEnumerable<TResult>> QueryPaginatedElementsAsync<TResult, TResultKey>(
            Func<IMongoQueryable<TModel>, IMongoQueryable<TResult>> filter,
            Expression<Func<TResult, TResultKey>> orderKeySelector,
            int page,
            int take,
            bool useDescendingOrder = false,
            CancellationToken cancellationToken = default)
        {
            var models = await QueryElementsAsync(elements =>

                useDescendingOrder ?

                filter(elements)
                    .PaginateDescending(orderKeySelector, page, take)
                    .ToListAsync(cancellationToken) :

                filter(elements)
                    .Paginate(orderKeySelector, page, take)
                    .ToListAsync(cancellationToken)).ConfigureAwait(false);

            var totalModels = await QueryElementsAsync(elements => filter(elements)
                .LongCountAsync(cancellationToken)).ConfigureAwait(false);

            return new PaginatedEnumerable<TResult>(
                models,
                totalModels,
                page,
                take);
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

        public async Task<object?> TryFindOneAsync(object id, CancellationToken cancellationToken = default) =>
            await TryFindOneAsync((TKey)id, cancellationToken).ConfigureAwait(false);

        public async Task<TModel?> TryFindOneAsync(
            TKey id,
            CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return null;
            }

            try
            {
                return await FindOneAsync(id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is FormatException or
                                           MongodmEntityNotFoundException)
            {
                return null;
            }
        }

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

        // Protected virtual methods.
        protected virtual Task CreateOnDBAsync(IEnumerable<TModel> models, CancellationToken cancellationToken) =>
            AccessToCollectionAsync(collection => collection.InsertManyAsync(models, null, cancellationToken));

        protected virtual Task CreateOnDBAsync(TModel model, CancellationToken cancellationToken) =>
            AccessToCollectionAsync(collection => collection.InsertOneAsync(model, null, cancellationToken));

        protected virtual Task DeleteOnDBAsync(TModel model, CancellationToken cancellationToken) =>
            AccessToCollectionAsync(collection =>
            {
                if (model is null)
                    throw new ArgumentNullException(nameof(model));

                return collection.DeleteOneAsync(
                    Builders<TModel>.Filter.Eq(m => m.Id, model.Id),
                    cancellationToken);
            });

        protected virtual async Task<TModel> FindOneOnDBAsync(TKey id, CancellationToken cancellationToken = default)
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
        private Task<TModel> FindOneOnDBAsync(
            Expression<Func<TModel, bool>> predicate,
            CancellationToken cancellationToken = default) =>
            AccessToCollectionAsync(async collection =>
            {
                if (predicate is null)
                    throw new ArgumentNullException(nameof(predicate));

                using var cursor = await collection.FindAsync(predicate, cancellationToken: cancellationToken).ConfigureAwait(false);
                var model = await cursor.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

                if (model == default(TModel))
                    throw new MongodmEntityNotFoundException("Can't find element");

                logger.RepositoryFoundDocument(Name, DbContext.Options.DbName, model.Id!.ToString());

                return model;
            });


        private Task ReplaceHelperAsync(
            TModel model,
            IClientSessionHandle? session,
            bool updateDependentDocuments,
            CancellationToken cancellationToken) =>
            AccessToCollectionAsync(async collection =>
            {
                if (model == null)
                    throw new ArgumentNullException(nameof(model));

                // Replace on db.
                if (session == null)
                {
                    await collection.ReplaceOneAsync(
                        Builders<TModel>.Filter.Eq(m => m.Id, model.Id),
                        model,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await collection.ReplaceOneAsync(
                        session,
                        Builders<TModel>.Filter.Eq(m => m.Id, model.Id),
                        model,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                // Update dependent documents.
                if (updateDependentDocuments)
                    DbContext.DbMaintainer.OnUpdatedModel<TKey>((IAuditable)model);

                // Reset changed members.
                ((IAuditable)model).ResetChangedMembers();

                logger.RepositoryReplacedDocument(Name, DbContext.Options.DbName, model.Id!.ToString());
            });
    }
}