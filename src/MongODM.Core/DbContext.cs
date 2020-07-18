using Etherna.MongODM.Migration;
using Etherna.MongODM.Models;
using Etherna.MongODM.Models.Internal;
using Etherna.MongODM.Models.Internal.ModelMaps;
using Etherna.MongODM.ProxyModels;
using Etherna.MongODM.Repositories;
using Etherna.MongODM.Serialization;
using Etherna.MongODM.Serialization.Modifiers;
using Etherna.MongODM.Utility;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            IDbDependencies dependencies,
            DbContextOptions options)
        {
            if (dependencies is null)
                throw new ArgumentNullException(nameof(dependencies));
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            ApplicationVersion = options.ApplicationVersion;
            DbCache = dependencies.DbCache;
            DbMaintainer = dependencies.DbMaintainer;
            DbMigrationManager = dependencies.DbMigrationManager;
            DbOperations = new CollectionRepository<OperationBase, string>(options.DbOperationsCollectionName);
            DocumentSchemaRegister = dependencies.DocumentSchemaRegister;
            Identifier = options.Identifier ?? GetType().Name;
            LibraryVersion = GetType()
                .GetTypeInfo()
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
                ?.Split('+')[0] ?? "1.0.0";
            ProxyGenerator = dependencies.ProxyGenerator;
            RepositoryRegister = dependencies.RepositoryRegister;
            SerializerModifierAccessor = dependencies.SerializerModifierAccessor;

            // Initialize MongoDB driver.
            Client = new MongoClient(options.ConnectionString);
            Database = Client.GetDatabase(options.DbName);

            // Initialize internal dependencies.
            DbMaintainer.Initialize(this);
            DbMigrationManager.Initialize(this);
            DocumentSchemaRegister.Initialize(this);
            RepositoryRegister.Initialize(this);

            // Initialize repositories.
            foreach (var repository in RepositoryRegister.ModelRepositoryMap.Values)
                repository.Initialize(this);

            // Register model maps.
            //internal maps
            new DbMigrationOperationMap().Register(this);
            new ModelBaseMap().Register(this);
            new OperationBaseMap().Register(this);
            new SeedOperationMap().Register(this);

            //application maps
            foreach (var maps in ModelMapsCollectors)
                maps.Register(this);

            // Build and freeze document schema register.
            DocumentSchemaRegister.Freeze();
        }

        // Public properties.
        public SemanticVersion ApplicationVersion { get; }
        public IReadOnlyCollection<IEntityModel> ChangedModelsList =>
            DbCache.LoadedModels.Values
                .Where(model => (model as IAuditable)?.IsChanged == true)
                .ToList();
        public IMongoClient Client { get; }
        public IMongoDatabase Database { get; }
        public IDbCache DbCache { get; }
        public IDbMaintainer DbMaintainer { get; }
        public IDbMigrationManager DbMigrationManager { get; }
        public ICollectionRepository<OperationBase, string> DbOperations { get; }
        public virtual IEnumerable<MongoMigrationBase> DocumentMigrationList { get; } = Array.Empty<MongoMigrationBase>();
        public IDocumentSchemaRegister DocumentSchemaRegister { get; }
        public string Identifier { get; }
        public SemanticVersion LibraryVersion { get; }
        public IProxyGenerator ProxyGenerator { get; }
        public IRepositoryRegister RepositoryRegister { get; }
        public ISerializerModifierAccessor SerializerModifierAccessor { get; }

        // Protected properties.
        protected abstract IEnumerable<IModelMapsCollector> ModelMapsCollectors { get; }

        // Methods.
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
                var modelType = ProxyGenerator.PurgeProxyType(model.GetType());
                while (modelType != typeof(object)) //try to find right collection. Can't replace model if it is stored on gridfs
                {
                    if (RepositoryRegister.ModelCollectionRepositoryMap.ContainsKey(modelType))
                    {
                        var repository = RepositoryRegister.ModelCollectionRepositoryMap[modelType];
                        await repository.ReplaceAsync(model).ConfigureAwait(false);
                        break;
                    }
                    else
                    {
                        modelType = modelType.BaseType;
                    }
                }
            }
        }

        public async Task<bool> SeedIfNeededAsync()
        {
            // Check if already seeded.
            if (await DbOperations.QueryElementsAsync(elements =>
                    elements.OfType<SeedOperation>()
                            .AnyAsync(sop => sop.DbContextName == Identifier)).ConfigureAwait(false))
                return false;

            // Seed.
            await SeedAsync().ConfigureAwait(false);

            // Report operation.
            var seedOperation = new SeedOperation(this);
            await DbOperations.CreateAsync(seedOperation).ConfigureAwait(false);

            return true;
        }

        public Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default) =>
            Client.StartSessionAsync(cancellationToken: cancellationToken);

        // Protected methods.
        protected virtual Task SeedAsync() =>
            Task.CompletedTask;
    }
}