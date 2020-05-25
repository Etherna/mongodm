using Etherna.MongODM.Migration;
using Etherna.MongODM.Models;
using Etherna.MongODM.ProxyModels;
using Etherna.MongODM.Repositories;
using Etherna.MongODM.Serialization;
using Etherna.MongODM.Serialization.Modifiers;
using Etherna.MongODM.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM
{
    public abstract class DbContext : IDbContext
    {
        // Consts.
        public const string DocumentVersionElementName = "v";

        // Constructors and initialization.
        public DbContext(
            IDbContextDependencies dependencies,
            DbContextOptions options)
        {
            DBCache = dependencies.DbCache;
            DBMaintainer = dependencies.DbMaintainer;
            DocumentSchemaRegister = dependencies.DocumentSchemaRegister;
            DocumentVersion = options.DocumentVersion;
            ProxyGenerator = dependencies.ProxyGenerator;
            RepositoryRegister = dependencies.RepositoryRegister;
            SerializerModifierAccessor = dependencies.SerializerModifierAccessor;

            // Initialize MongoDB driver.
            Client = new MongoClient(options.ConnectionString);
            Database = Client.GetDatabase(options.DBName);

            // Initialize internal dependencies.
            DocumentSchemaRegister.Initialize(this);
            DBMaintainer.Initialize(this);
            RepositoryRegister.Initialize(this);

            // Initialize repositories.
            foreach (var repository in RepositoryRegister.ModelRepositoryMap.Values)
                repository.Initialize(this);

            // Customize conventions.
            ConventionRegistry.Register("Enum string", new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String)
            }, c => true);

            // Register serializers.
            foreach (var serializerCollector in SerializerCollectors)
                serializerCollector.Register(this);

            // Build and freeze document schema register.
            DocumentSchemaRegister.Freeze();
        }

        // Public properties.
        public IReadOnlyCollection<IEntityModel> ChangedModelsList =>
            DBCache.LoadedModels.Values
                .Where(model => (model as IAuditable)?.IsChanged == true)
                .ToList();
        public IMongoClient Client { get; }
        public IMongoDatabase Database { get; }
        public IDbCache DBCache { get; }
        public IDbMaintainer DBMaintainer { get; }
        public IDocumentSchemaRegister DocumentSchemaRegister { get; }
        public DocumentVersion DocumentVersion { get; }
        public bool IsMigrating { get; private set; }
        public IProxyGenerator ProxyGenerator { get; }
        public IRepositoryRegister RepositoryRegister { get; }
        public ISerializerModifierAccessor SerializerModifierAccessor { get; }

        // Protected properties.
        protected virtual IEnumerable<MongoMigrationBase> MigrationTaskList { get; } = Array.Empty<MongoMigrationBase>();
        protected abstract IEnumerable<IModelSerializerCollector> SerializerCollectors { get; }

        // Methods.
        public async Task MigrateRepositoriesAsync(CancellationToken cancellationToken = default)
        {
            IsMigrating = true;

            // Migrate collections.
            foreach (var migration in MigrationTaskList)
                await migration.MigrateAsync(cancellationToken);

            // Build indexes.
            foreach (var repository in RepositoryRegister.ModelCollectionRepositoryMap.Values)
                await repository.BuildIndexesAsync(DocumentSchemaRegister, cancellationToken);

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
                if (RepositoryRegister.ModelCollectionRepositoryMap.ContainsKey(modelType)) //can't replace if is a file
                {
                    var repository = RepositoryRegister.ModelCollectionRepositoryMap[modelType];
                    await repository.ReplaceAsync(model);
                }
            }
        }

        public Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default) =>
            Client.StartSessionAsync(cancellationToken: cancellationToken);
    }
}