using Digicando.ExecContext;
using Digicando.MongODM.Models;
using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Repositories;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongODM
{
    public abstract class DbContext : IDbContext
    {
        // Consts.
        public const string DocumentVersionElementName = "v";

        // Constructor.
        public DbContext(
            IDBCache dbCache,
            IDBMaintainer dbMaintainer,
            IDocumentSchemaRegister documentSchemaRegister,
            DbContextOptions options,
            IProxyGenerator proxyGenerator)
        {
            DBCache = dbCache;
            DBMaintainer = dbMaintainer;
            DocumentSchemaRegister = documentSchemaRegister;
            DocumentVersion = options.DocumentVersion;
            ExecContextAccessor = new CurrentContextAccessor(options.ExecContextAccessors);
            ProxyGenerator = proxyGenerator;

            // Initialize MongoDB driver.
            Client = new MongoClient(options.ConnectionString);
            Database = Client.GetDatabase(options.DBName);

            // Init IoC dependencies.
            documentSchemaRegister.Initialize(this);

            // Customize conventions.
            ConventionRegistry.Register("Enum string", new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String)
            }, c => true);

            // Register serializers.
            foreach (var serializerCollector in SerializerCollectors)
                serializerCollector.Register(this);

            // Build and freeze document schema register.
            documentSchemaRegister.Freeze();
        }

        // Public properties.
        public IReadOnlyCollection<IEntityModel> ChangedModelsList =>
            DBCache.LoadedModels.Values
                .Where(model => (model as IAuditable).IsChanged)
                .ToList();
        public IMongoClient Client { get; }
        public IMongoDatabase Database { get; }
        public IDBCache DBCache { get; }
        public IDBMaintainer DBMaintainer { get; }
        public IDocumentSchemaRegister DocumentSchemaRegister { get; }
        public DocumentVersion DocumentVersion { get; }
        public ICurrentContextAccessor ExecContextAccessor { get; }
        public bool IsMigrating { get; private set; }
        public abstract IReadOnlyDictionary<Type, ICollectionRepository> ModelCollectionRepositoryMap { get; }
        public abstract IReadOnlyDictionary<Type, IGridFSRepository> ModelGridFSRepositoryMap { get; }
        public IReadOnlyDictionary<Type, IRepository> ModelRepositoryMap =>
            Enumerable.Union<(Type ModelType, IRepository Repository)>(
                ModelCollectionRepositoryMap.Select(pair => (pair.Key, pair.Value as IRepository)),
                ModelGridFSRepositoryMap.Select(pair => (pair.Key, pair.Value as IRepository)))
            .ToDictionary(pair => pair.ModelType, pair => pair.Repository);
        public IProxyGenerator ProxyGenerator { get; }

        // Protected properties.
        protected abstract IEnumerable<IModelSerializerCollector> SerializerCollectors { get; }

        // Methods.
        public async Task MigrateRepositoriesAsync()
        {
            // Migrate documents.
            IsMigrating = true;
            foreach (var migration in from repository in ModelCollectionRepositoryMap.Values
                                      where repository.MigrationInfo != null
                                      orderby repository.MigrationInfo.PriorityIndex
                                      select repository.MigrationInfo)
                await migration.MigrateAsync();

            // Build indexes.
            foreach (var repository in ModelCollectionRepositoryMap.Values)
                await repository.BuildIndexesAsync(DocumentSchemaRegister);

            IsMigrating = false;
        }

        public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            /*
             * Currently at MongoDB 4.0 sessions are only available for Replica Sets.
             * This exclude the development environment from use them, so in order to have a more
             * similar set up in development and production it's better to disable them, for now.
             */

            //using (var session = await StartSessionAsync())
            //{
            //    session.StartTransaction();

            //    try
            //    {
            //        // Commit updated models replacement.
            //        foreach (var model in DBCache.LoadedModels.Values
            //            .Where(model => (model as IAuditable).IsChanged)
            //            .ToList())
            //        {
            //            var repository = ModelCollectionRepositoryMap[model.GetType().BaseType];
            //            await repository.ReplaceAsync(model, session);
            //        }
            //    }
            //    catch
            //    {
            //        await session.AbortTransactionAsync();
            //        throw;
            //    }

            //    await session.CommitTransactionAsync();
            //}

            // Commit updated models replacement.
            foreach (var model in ChangedModelsList)
            {
                var modelType = model.GetType().BaseType;
                if (ModelCollectionRepositoryMap.ContainsKey(modelType)) //can't replace if is a file
                {
                    var repository = ModelCollectionRepositoryMap[modelType];
                    await repository.ReplaceAsync(model);
                }
            }
        }

        public Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default) =>
            Client.StartSessionAsync(cancellationToken: cancellationToken);
    }
}