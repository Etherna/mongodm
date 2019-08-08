using Digicando.MongoDM.ProxyModels;
using Digicando.MongoDM.Repositories;
using Digicando.MongoDM.Serialization;
using Digicando.MongoDM.Utility;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongoDM
{
    public interface IDBContextBase
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