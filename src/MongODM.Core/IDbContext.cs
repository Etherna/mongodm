using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Repositories;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Serialization.Modifiers;
using Digicando.MongODM.Utility;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace Digicando.MongODM
{
    /// <summary>
    /// Interface of <see cref="DbContext"/> implementation.
    /// </summary>
    public interface IDbContext
    {
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
        IDBCache DBCache { get; }

        /// <summary>
        /// Database operator interested into maintenance tasks.
        /// </summary>
        IDBMaintainer DBMaintainer { get; }
        
        /// <summary>
        /// Container for model serialization and document schema information.
        /// </summary>
        IDocumentSchemaRegister DocumentSchemaRegister { get; }
        
        /// <summary>
        /// Current operating document version.
        /// </summary>
        DocumentVersion DocumentVersion { get; }
        
        /// <summary>
        /// Flag reporting eventual current migration operation.
        /// </summary>
        bool IsMigrating { get; }
        
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