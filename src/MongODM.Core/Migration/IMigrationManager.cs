using Etherna.MongODM.Operations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Migration
{
    internal interface IMigrationManager
    {
        Task<MigrateOperation?> IsMigrationRunningAsync();

        Task<List<MigrateOperation>> GetLastMigrationsAsync(int page, int take);

        Task<MigrateOperation> GetMigrationAsync(string migrateOperationId);

        /// <summary>
        /// Start a database migration process.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task MigrateRepositoriesAsync(CancellationToken cancellationToken = default);
    }
}