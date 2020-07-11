using Etherna.MongODM.ProxyModels;
using Etherna.MongODM.Repositories;
using Etherna.MongODM.Serialization;
using Etherna.MongODM.Serialization.Modifiers;
using Etherna.MongODM.Utility;
using MongoDB.Driver;
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
        IDbCache DbCache { get; }

        /// <summary>
        /// Database operator interested into maintenance tasks.
        /// </summary>
        IDbMaintainer DbMaintainer { get; }
        
        /// <summary>
        /// Container for model serialization and document schema information.
        /// </summary>
        IDocumentSchemaRegister DocumentSchemaRegister { get; }

        /// <summary>
        /// DbContext unique identifier.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Flag reporting eventual current migration operation.
        /// </summary>
        bool IsMigrating { get; }

        /// <summary>
        /// Current MongODM library version
        /// </summary>
        SemanticVersion LibraryVersion { get; }

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
        /// Start a database migration process.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task MigrateRepositoriesAsync(CancellationToken cancellationToken = default);

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