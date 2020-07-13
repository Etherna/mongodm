using Etherna.MongODM.Models.Internal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etherna.MongODM.Utility
{
    public interface IDbContextMigrationManager
    {
        Task<MigrateOperation?> IsMigrationRunningAsync();

        Task<List<MigrateOperation>> GetLastMigrationsAsync(int page, int take);

        Task<MigrateOperation> GetMigrationAsync(string migrateOperationId);

        /// <summary>
        /// Start a db context migration process.
        /// </summary>
        /// <param name="authorId">Id of user requiring the migration</param>
        Task StartDbContextMigrationAsync(string authorId);
    }
}