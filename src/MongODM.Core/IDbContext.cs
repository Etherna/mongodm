﻿using Etherna.MongODM.Migration;
using Etherna.MongODM.Operations;
using Etherna.MongODM.ProxyModels;
using Etherna.MongODM.Repositories;
using Etherna.MongODM.Serialization;
using Etherna.MongODM.Serialization.Modifiers;
using Etherna.MongODM.Utility;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM
{
    /// <summary>
    /// Interface of <see cref="DbContext"/> implementation.
    /// </summary>
    public interface IDbContext
    {
        /// <summary>
        /// Current application version.
        /// </summary>
        SemanticVersion ApplicationVersion { get; }

        // Properties.
        /// <summary>
        /// Current MongoDB client.
        /// </summary>
        IMongoClient Client { get; }
        
        /// <summary>
        /// Current MongoDB database.
        /// </summary>
        IMongoDatabase Database { get; }
        
        /// <summary>
        /// Database cache container.
        /// </summary>
        IDbContextCache DbCache { get; }

        /// <summary>
        /// Database operator interested into maintenance tasks.
        /// </summary>
        IDbContextMaintainer DbMaintainer { get; }

        /// <summary>
        /// Manage migrations over database context
        /// </summary>
        IDbContextMigrationManager DbContextMigrationManager { get; }

        /// <summary>
        /// Internal collection for keep db operations execution log
        /// </summary>
        ICollectionRepository<OperationBase, string> DbOperations { get; }

        /// <summary>
        /// Container for model serialization and document schema information.
        /// </summary>
        IDocumentSchemaRegister DocumentSchemaRegister { get; }

        /// <summary>
        /// DbContext unique identifier.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Current MongODM library version
        /// </summary>
        SemanticVersion LibraryVersion { get; }

        /// <summary>
        /// List of registered migration tasks
        /// </summary>
        IEnumerable<MongoMigrationBase> MigrationTaskList { get; }

        /// <summary>
        /// Current model proxy generator.
        /// </summary>
        IProxyGenerator ProxyGenerator { get; }

        /// <summary>
        /// Register of available repositories.
        /// </summary>
        IRepositoryRegister RepositoryRegister { get; }

        /// <summary>
        /// Serializer modifier accessor.
        /// </summary>
        ISerializerModifierAccessor SerializerModifierAccessor { get; }

        // Methods.
        /// <summary>
        /// Save current model changes on db.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Start a new database transaction session.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The session handler</returns>
        Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default);
    }
}