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

using Etherna.ExecContext;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Driver;
using Etherna.MongoDB.Driver.Linq;
using Etherna.MongODM.Core.Domain.ModelMaps;
using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Exceptions;
using Etherna.MongODM.Core.Migration;
using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core
{
    public abstract class DbContext : IDbContext, IDbContextBuilder
    {
        // Fields.
        private bool? _isSeeded;
        private BsonSerializerRegistry _serializerRegistry = default!;
        private bool isInitialized;
        private readonly ReaderWriterLockSlim isSeededLock = new(); //support read/write locks
        private readonly SemaphoreSlim seedingSemaphore = new(1, 1); //support async/await

        // Constructor and initializer.
        protected DbContext() { }
        public void Initialize(
            IDbDependencies dependencies,
            IDbContextOptions options)
        {
            if (isInitialized)
                throw new InvalidOperationException("DbContext already initialized");
            if (dependencies is null)
                throw new ArgumentNullException(nameof(dependencies));
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            // Set dependencies.
            DbCache = dependencies.DbCache;
            DbMaintainer = dependencies.DbMaintainer;
            DbMigrationManager = dependencies.DbMigrationManager;
            DbOperations = new CollectionRepository<OperationBase, string>(options.DbOperationsCollectionName);
            DiscriminatorRegistry = dependencies.DiscriminatorRegistry;
            ExecutionContext = dependencies.ExecutionContext;
            LibraryVersion = typeof(DbContext)
                .GetTypeInfo()
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
                ?.Split('+')[0] ?? "1.0.0";
            Options = options;
            ProxyGenerator = dependencies.ProxyGenerator;
            RepositoryRegistry = dependencies.RepositoryRegistry;
            SchemaRegistry = dependencies.SchemaRegistry;
            SerializerModifierAccessor = dependencies.SerializerModifierAccessor;
            _serializerRegistry = (BsonSerializerRegistry)dependencies.BsonSerializerRegistry;

            // Initialize MongoDB driver.
            Client = new MongoClient(options.ConnectionString);
            Database = Client.GetDatabase(options.DbName, new MongoDatabaseSettings
            {
                SerializerRegistry = _serializerRegistry
            });

            // Initialize internal dependencies.
            DbMaintainer.Initialize(this);
            DbMigrationManager.Initialize(this);
            DiscriminatorRegistry.Initialize(this);
            RepositoryRegistry.Initialize(this);
            SchemaRegistry.Initialize(this);
            InitializeSerializerRegistry();

            // Initialize repositories.
            foreach (var repository in RepositoryRegistry.ModelRepositoryMap.Values)
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

            // Build and freeze schema registry.
            SchemaRegistry.Freeze();

            // Set as initialized.
            isInitialized = true;
        }

        // Public properties.
        public IReadOnlyCollection<IEntityModel> ChangedModelsList =>
            DbCache.LoadedModels.Values
                .Where(model => (model as IAuditable)?.IsChanged == true)
                .ToList();
        public IMongoClient Client { get; private set; } = default!;
        public IMongoDatabase Database { get; private set; } = default!;
        public IDbCache DbCache { get; private set; } = default!;
        public IDbMaintainer DbMaintainer { get; private set; } = default!;
        public IDbMigrationManager DbMigrationManager { get; private set; } = default!;
        public ICollectionRepository<OperationBase, string> DbOperations { get; private set; } = default!;
        public IDiscriminatorRegistry DiscriminatorRegistry { get; private set; } = default!;
        public virtual IEnumerable<DocumentMigration> DocumentMigrationList { get; } = Array.Empty<DocumentMigration>();
        public IExecutionContext ExecutionContext { get; private set; } = default!;
        public string Identifier => Options?.Identifier ?? GetType().Name;
        public bool IsSeeded
        {
            get
            {
                // Try to read cached.
                isSeededLock.EnterReadLock();
                try
                {
                    if (_isSeeded.HasValue)
                        return _isSeeded.Value;
                }
                finally
                {
                    isSeededLock.ExitReadLock();
                }

                // Get seeding state from db.
                isSeededLock.EnterWriteLock();
                try
                {
                    if (!_isSeeded.HasValue)
                    {
                        var task = DbOperations.QueryElementsAsync(elements =>
                                elements.OfType<SeedOperation>()
                                        .AnyAsync(sop => sop.DbContextName == Identifier));
                        task.Wait();
                        _isSeeded = task.Result;
                    }

                    return _isSeeded.Value;
                }
                finally
                {
                    isSeededLock.ExitWriteLock();
                }
            }
            private set
            {
                isSeededLock.EnterWriteLock();
                try
                {
                    _isSeeded = value;
                }
                finally
                {
                    isSeededLock.ExitWriteLock();
                }
            }
        }
        public SemanticVersion LibraryVersion { get; private set; } = default!;
        public IDbContextOptions Options { get; private set; } = default!;
        public IProxyGenerator ProxyGenerator { get; private set; } = default!;
        public IRepositoryRegistry RepositoryRegistry { get; private set; } = default!;
        public IBsonSerializerRegistry SerializerRegistry => _serializerRegistry;
        public ISchemaRegistry SchemaRegistry { get; private set; } = default!;
        public ISerializerModifierAccessor SerializerModifierAccessor { get; private set; } = default!;

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
                    if (RepositoryRegistry.ModelCollectionRepositoryMap.ContainsKey(modelType))
                    {
                        var repository = RepositoryRegistry.ModelCollectionRepositoryMap[modelType];
                        await repository.ReplaceAsync(model, cancellationToken: cancellationToken).ConfigureAwait(false);
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
            if (IsSeeded)
                return false;

            await seedingSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // Check again if seeded.
                if (IsSeeded)
                    return false;

                // Seed.
                try { await SeedAsync().ConfigureAwait(false); }
                catch (Exception e) { throw new MongodmDbSeedingException($"Error seeding {GetType().Name} dbContext", e); }

                // Report operation.
                var seedOperation = new SeedOperation(this);
                await DbOperations.CreateAsync(seedOperation).ConfigureAwait(false);

                // Cache as seeded.
                IsSeeded = true;

                return true;
            }
            finally
            {
                seedingSemaphore.Release();
            }
        }

        public Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default) =>
            Client.StartSessionAsync(cancellationToken: cancellationToken);

        // Protected methods.
        protected virtual Task SeedAsync() =>
            Task.CompletedTask;

        // Private helpers.
        private void InitializeSerializerRegistry()
        {
            //order matters. It's in reverse order of how they'll get consumed
            _serializerRegistry.RegisterSerializationProvider(new DiscriminatedInterfaceSerializationProvider());
            _serializerRegistry.RegisterSerializationProvider(new CollectionsSerializationProvider());
            _serializerRegistry.RegisterSerializationProvider(new PrimitiveSerializationProvider());
            _serializerRegistry.RegisterSerializationProvider(new AttributedSerializationProvider());
            _serializerRegistry.RegisterSerializationProvider(new BsonObjectModelSerializationProvider());
        }
    }
}