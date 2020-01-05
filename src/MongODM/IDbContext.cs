using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Repositories;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Utility;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongODM
{
    public interface IDbContext
    {
        // Properties.
        IMongoClient Client { get; }
        IMongoDatabase Database { get; }
        IDBCache DBCache { get; }
        IDBMaintainer DBMaintainer { get; }
        IDocumentSchemaRegister DocumentSchemaRegister { get; }
        DocumentVersion DocumentVersion { get; }
        bool IsMigrating { get; }
        IReadOnlyDictionary<Type, ICollectionRepository> ModelCollectionRepositoryMap { get; }
        IReadOnlyDictionary<Type, IGridFSRepository> ModelGridFSRepositoryMap { get; }
        IReadOnlyDictionary<Type, IRepository> ModelRepositoryMap { get; }
        IProxyGenerator ProxyGenerator { get; }

        // Methods.
        Task MigrateRepositoriesAsync();

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default);
    }
}