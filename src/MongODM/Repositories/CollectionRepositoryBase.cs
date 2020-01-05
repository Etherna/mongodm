using Digicando.MongODM.Models;
using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Utility;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongODM.Repositories
{
    public abstract class CollectionRepositoryBase<TModel, TKey> :
        RepositoryBase<TModel, TKey>,
        ICollectionRepository<TModel, TKey>
        where TModel : class, IEntityModel<TKey>
    {
        // Fields.
        private readonly IDBMaintainer dbMaintainer;
        private readonly IDocumentSchemaRegister documentSchemaRegister;

        // Constructors.
        public CollectionRepositoryBase(
            string collectionName,
            IDbContext dbContext)
            : base(dbContext)
        {
            Collection = dbContext.Database.GetCollection<TModel>(collectionName);
            dbMaintainer = dbContext.DBMaintainer;
            documentSchemaRegister = dbContext.DocumentSchemaRegister;
        }

        // Properties.
        public IMongoCollection<TModel> Collection { get; }
        protected virtual IEnumerable<(IndexKeysDefinition<TModel> keys, CreateIndexOptions<TModel> options)> IndexBuilders =>
            new (IndexKeysDefinition<TModel> keys, CreateIndexOptions<TModel> options)[0];

        // Public methods.
        public override async Task BuildIndexesAsync(IDocumentSchemaRegister schemaRegister)
        {
            var newIndexes = new List<(string name, CreateIndexModel<TModel> createIndex)>();

            // Define new indexes.
            //repository defined
            newIndexes.AddRange(IndexBuilders.Select(pair =>
            {
                var (keys, options) = pair;
                if (options.Name == null)
                {
                    var renderedKeys = keys.Render(Collection.DocumentSerializer, Collection.Settings.SerializerRegistry);
                    options.Name = $"doc_{ string.Join("_", renderedKeys.Names) }";
                }

                return (options.Name, new CreateIndexModel<TModel>(keys, options));
            }));

            //root document
            newIndexes.Add(
                ("ver",
                 new CreateIndexModel<TModel>(
                    Builders<TModel>.IndexKeys.Ascending(DbContext.DocumentVersionElementName),
                    new CreateIndexOptions { Name = "ver" })));

            //referenced documents
            var dependencies = documentSchemaRegister.GetModelEntityReferencesIds(typeof(TModel));

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
            using (var indexList = await Collection.Indexes.ListAsync())
                while (indexList.MoveNext())
                    currentIndexes.AddRange(indexList.Current);

            // Remove old indexes.
            foreach (var oldIndex in from index in currentIndexes
                                     let indexName = index.GetElement("name").Value.ToString()
                                     where indexName != "_id_"
                                     where !newIndexes.Any(newIndex => newIndex.name == indexName)
                                     select index)
            {
                await Collection.Indexes.DropOneAsync(oldIndex.GetElement("name").Value.ToString());
            }

            // Build new indexes.
            await Collection.Indexes.CreateManyAsync(newIndexes.Select(i => i.createIndex));
        }

        public virtual Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(
            FilterDefinition<TModel> filter,
            FindOptions<TModel, TProjection> options = null,
            CancellationToken cancellationToken = default) =>
            Collection.FindAsync(filter, options, cancellationToken);

        public virtual Task<TResult> QueryElementsAsync<TResult>(
            Func<IMongoQueryable<TModel>, Task<TResult>> query,
            AggregateOptions aggregateOptions = null) =>
            query(Collection.AsQueryable(aggregateOptions));

        public virtual Task ReplaceAsync(
            object model,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default) =>
            ReplaceAsync(model as TModel, updateDependentDocuments, cancellationToken);

        public virtual Task ReplaceAsync(
            object model,
            IClientSessionHandle session,
            bool updateDependentDocuments = true,
            CancellationToken cancellationToken = default) =>
            ReplaceAsync(model as TModel, session, updateDependentDocuments, cancellationToken);

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

        // Protected methods.
        protected override Task CreateOnDBAsync(IEnumerable<TModel> models, CancellationToken cancellationToken) =>
            Collection.InsertManyAsync(models, null, cancellationToken);

        protected override Task CreateOnDBAsync(TModel model, CancellationToken cancellationToken) =>
            Collection.InsertOneAsync(model, null, cancellationToken);

        protected override Task DeleteOnDBAsync(TModel model, CancellationToken cancellationToken) =>
            Collection.DeleteOneAsync(
                Builders<TModel>.Filter.Eq(m => m.Id, model.Id),
                cancellationToken);

        protected override async Task<TModel> FindOneOnDBAsync(TKey id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var filter = Builders<TModel>.Filter.Eq(m => m.Id, id);
            var element = await Collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
            if (element as TModel == default(TModel))
                throw new KeyNotFoundException($"Can't find key {id}");

            return element as TModel;
        }

        // Helpers.
        private async Task ReplaceHelperAsync(
            TModel model,
            IClientSessionHandle session,
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
                    cancellationToken: cancellationToken);
            }
            else
            {
                await Collection.ReplaceOneAsync(
                    session,
                    Builders<TModel>.Filter.Eq(m => m.Id, model.Id),
                    model,
                    cancellationToken: cancellationToken);
            }

            // Update dependent documents.
            if (updateDependentDocuments)
                dbMaintainer.OnUpdatedModel(model as IAuditable, model.Id);

            // Reset changed members.
            (model as IAuditable).ResetChangedMembers();
        }
    }
}