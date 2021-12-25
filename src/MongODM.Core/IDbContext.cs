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
using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Migration;
using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Utility;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core
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
        IDbCache DbCache { get; }

        /// <summary>
        /// Database operator interested into maintenance tasks.
        /// </summary>
        IDbMaintainer DbMaintainer { get; }

        /// <summary>
        /// Manage migrations over database context
        /// </summary>
        IDbMigrationManager DbMigrationManager { get; }

        /// <summary>
        /// Internal collection for keep db operations execution log
        /// </summary>
        ICollectionRepository<OperationBase, string> DbOperations { get; }

        /// <summary>
        /// Registry for discriminator configuration.
        /// </summary>
        IDiscriminatorRegistry DiscriminatorRegistry { get; }

        /// <summary>
        /// List of registered migration tasks
        /// </summary>
        IEnumerable<DocumentMigration> DocumentMigrationList { get; }

        /// <summary>
        /// DbContext unique identifier.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// True if it has been seeded.
        /// </summary>
        bool IsSeeded { get; }

        /// <summary>
        /// Current MongODM library version.
        /// </summary>
        SemanticVersion LibraryVersion { get; }

        /// <summary>
        /// Db context options.
        /// </summary>
        IDbContextOptions Options { get; }

        /// <summary>
        /// Current model proxy generator.
        /// </summary>
        IProxyGenerator ProxyGenerator { get; }

        /// <summary>
        /// Registry of available repositories.
        /// </summary>
        IRepositoryRegistry RepositoryRegistry { get; }

        /// <summary>
        /// Local instance of a serializer registry.
        /// </summary>
        IBsonSerializerRegistry SerializerRegistry { get; }

        /// <summary>
        /// Registry for model serialization and schema information.
        /// </summary>
        ISchemaRegistry SchemaRegistry { get; }

        /// <summary>
        /// Serializer modifier accessor.
        /// </summary>
        ISerializerModifierAccessor SerializerModifierAccessor { get; }

        /// <summary>
        /// ExecutionContext handler.
        /// </summary>
        IExecutionContext ExecutionContext { get; }

        // Methods.
        /// <summary>
        /// Save current model changes on db.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Seed database context if still not seeded
        /// </summary>
        /// <returns>True if seed has been executed. False otherwise</returns>
        Task<bool> SeedIfNeededAsync();

        /// <summary>
        /// Start a new database transaction session.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The session handler</returns>
        Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default);
    }
}