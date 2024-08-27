﻿// Copyright 2020-present Etherna SA
// This file is part of MongODM.
// 
// MongODM is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// MongODM is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with MongODM.
// If not, see <https://www.gnu.org/licenses/>.

using Etherna.ExecContext;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Driver;
using Etherna.MongoDB.Driver.Linq;
using Etherna.MongODM.Core.Domain.ModelMaps;
using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Exceptions;
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Migration;
using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Serialization.Providers;
using Etherna.MongODM.Core.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core
{
    public abstract class DbContext : IDbContext, IDbContextBuilder, IDisposable
    {
        // Fields.
        private bool? _isSeeded;
        private BsonSerializerRegistry _serializerRegistry = default!;
        private IEnumerable<IDbContext> childDbContexts = default!;
        private bool disposed;
        private bool isInitialized;
        private readonly ReaderWriterLockSlim isSeededLock = new(); //support read/write locks
        private readonly ILogger logger;
        private readonly SemaphoreSlim seedingSemaphore = new(1, 1); //support async/await

        // Constructor and initializer.
        protected DbContext(ILogger? logger = null)
        {
            this.logger = logger ?? NullLogger.Instance;
        }

        public void Initialize(
            IDbDependencies dependencies,
            IMongoClient mongoClient,
            IDbContextOptions options,
            IEnumerable<IDbContext> childDbContexts)
        {
            if (isInitialized)
                throw new InvalidOperationException("DbContext already initialized");
            ArgumentNullException.ThrowIfNull(dependencies, nameof(dependencies));
            ArgumentNullException.ThrowIfNull(options, nameof(options));

            // Set dependencies.
            this.childDbContexts = childDbContexts;
            DbCache = dependencies.DbCache;
            DbMaintainer = dependencies.DbMaintainer;
            DbMigrationManager = dependencies.DbMigrationManager;
            DbOperations = new Repository<OperationBase, string>(options.DbOperationsCollectionName);
            DiscriminatorRegistry = dependencies.DiscriminatorRegistry;
            ExecutionContext = dependencies.ExecutionContext;
            MapRegistry = dependencies.MapRegistry;
            Options = options;
            ProxyGenerator = dependencies.ProxyGenerator;
            RepositoryRegistry = dependencies.RepositoryRegistry;
            SerializerModifierAccessor = dependencies.SerializerModifierAccessor;
            _serializerRegistry = (BsonSerializerRegistry)dependencies.BsonSerializerRegistry;

            // Execute initialization into execution context.
            using var dbExecutionContext = new DbExecutionContextHandler(this);

            // Initialize internal dependencies.
            DbCache.Initialize(this, logger);
            DbMaintainer.Initialize(this, logger);
            DbMigrationManager.Initialize(this, logger);
            DiscriminatorRegistry.Initialize(this, logger);
            MapRegistry.Initialize(this, logger);
            RepositoryRegistry.Initialize(this, logger);
            InitializeSerializerRegistry();

            // Initialize repositories.
            foreach (var repository in RepositoryRegistry.Repositories)
                repository.Initialize(this, logger);

            // Register model maps.
            //internal maps
            new DbMigrationOperationMap().Register(this);
            new ModelBaseMap().Register(this);
            new OperationBaseMap().Register(this);
            new SeedOperationMap().Register(this);

            //application maps
            foreach (var maps in ModelMapsCollectors)
                maps.Register(this);

            // Build and freeze map registry.
            MapRegistry.Freeze();

            // Initialize MongoDB database.
            Client = mongoClient;
            Database = Client.GetDatabase(options.DbName, new MongoDatabaseSettings
            {
                SerializerRegistry = _serializerRegistry
            });

            // Set as initialized.
            isInitialized = true;

            logger.DbContextInitialized(options.DbName);
        }
        
        // Dispose.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            // Dispose managed resources.
            if (disposing)
            {
                isSeededLock.Dispose();
                seedingSemaphore.Dispose();
            }

            disposed = true;
        }

        // Public properties.
        public IReadOnlyCollection<IEntityModel> ChangedModelsList =>
            DbCache.LoadedModels.Values
                .Where(model => model is IAuditable { IsChanged: true })
                .ToList();
        public IMongoClient Client { get; private set; } = default!;
        public IMongoDatabase Database { get; private set; } = default!;
        public IDbCache DbCache { get; private set; } = default!;
        public IDbMaintainer DbMaintainer { get; private set; } = default!;
        public IDbMigrationManager DbMigrationManager { get; private set; } = default!;
        public IRepository<OperationBase, string> DbOperations { get; private set; } = default!;
        public IDiscriminatorRegistry DiscriminatorRegistry { get; private set; } = default!;
        public virtual IEnumerable<DocumentMigration> DocumentMigrationList { get; } = Array.Empty<DocumentMigration>();
        public IExecutionContext ExecutionContext { get; private set; } = default!;
        public string Identifier => Options.Identifier ?? GetType().Name;
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
        public IMapRegistry MapRegistry { get; private set; } = default!;
        public IDbContextOptions Options { get; private set; } = default!;
        public IProxyGenerator ProxyGenerator { get; private set; } = default!;
        public IRepositoryRegistry RepositoryRegistry { get; private set; } = default!;
        public IBsonSerializerRegistry SerializerRegistry => _serializerRegistry;
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

                var repository = RepositoryRegistry.TryGetRepositoryByHandledModelType(modelType);
                if (repository != null)
                {
                    await repository.ReplaceAsync(model, cancellationToken: cancellationToken).ConfigureAwait(false);

                    logger.DbContextSavedChangedModelToRepository(Options.DbName, repository.ModelIdToString(model), repository.Name);
                }
            }

            // Save changes on child dbcontexts.
            foreach (var child in childDbContexts)
            {
                await child.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            logger.DbContextSavedChanges(Options.DbName);
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

                logger.DbContextSeeded(Options.DbName);

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
            _serializerRegistry.RegisterSerializationProvider(new ModelMapSerializationProvider(this));
            _serializerRegistry.RegisterSerializationProvider(new DiscriminatedInterfaceSerializationProvider());
            _serializerRegistry.RegisterSerializationProvider(new CollectionsSerializationProvider());
            _serializerRegistry.RegisterSerializationProvider(new PrimitiveSerializationProvider());
            _serializerRegistry.RegisterSerializationProvider(new AttributedSerializationProvider());
            _serializerRegistry.RegisterSerializationProvider(new TypeMappingSerializationProvider());
            _serializerRegistry.RegisterSerializationProvider(new BsonObjectModelSerializationProvider());
        }
    }
}