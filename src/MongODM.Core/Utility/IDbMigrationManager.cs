using Etherna.MongODM.Models.Internal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etherna.MongODM.Utility
{
    public interface IDbMigrationManager
    {
        Task<DbMigrationOperation?> IsMigrationRunningAsync();

        Task<List<DbMigrationOperation>> GetLastMigrationsAsync(int page, int take);

        Task<DbMigrationOperation> GetMigrationAsync(string migrateOperationId);

        /// <summary>
        /// Start a db context migration process.
        /// </summary>
        /// <param name="authorId">Id of user requiring the migration</param>
        Task StartDbContextMigrationAsync(string authorId);
    }
}