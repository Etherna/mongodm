using Etherna.MongODM.Extensions;
using Etherna.MongODM.Models.Internal;
using Etherna.MongODM.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etherna.MongODM.Utility
{
    public class DbMigrationManager : IDbMigrationManager, IDbContextInitializable
    {
        // Fields.
        private IDbContext dbContext = default!;
        private readonly ITaskRunner taskRunner;

        // Constructor and initialization.
        public DbMigrationManager(ITaskRunner taskRunner)
        {
            this.taskRunner = taskRunner;
        }
        public void Initialize(IDbContext dbContext)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");

            this.dbContext = dbContext;

            IsInitialized = true;
        }

        // Properties.
        public bool IsInitialized { get; private set; }

        // Methods.
        public async Task<List<DbMigrationOperation>> GetLastMigrationsAsync(int page, int take) =>
            await dbContext.DbOperations.QueryElementsAsync(elements =>
                elements.OfType<DbMigrationOperation>()
                        .PaginateDescending(r => r.CreationDateTime, page, take)
                        .ToListAsync()).ConfigureAwait(false);

        public async Task<DbMigrationOperation> GetMigrationAsync(string migrateOperationId)
        {
            if (migrateOperationId is null)
                throw new ArgumentNullException(nameof(migrateOperationId));

            var migrateOp = await dbContext.DbOperations.QueryElementsAsync(elements =>
                elements.OfType<DbMigrationOperation>()
                        .Where(op => op.Id == migrateOperationId)
                        .FirstAsync()).ConfigureAwait(false);

            return migrateOp;
        }

        public async Task<DbMigrationOperation?> IsMigrationRunningAsync()
        {
            var migrateOp = await dbContext.DbOperations.QueryElementsAsync(elements =>
                elements.OfType<DbMigrationOperation>()
                        .Where(op => op.DbContextName == dbContext.Identifier)
                        .Where(op => op.CurrentStatus == DbMigrationOperation.Status.Running)
                        .FirstOrDefaultAsync()).ConfigureAwait(false);

            return migrateOp;
        }

        public async Task StartDbContextMigrationAsync(string authorId)
        {
            var migrateOp = new DbMigrationOperation(dbContext, authorId);
            await dbContext.DbOperations.CreateAsync(migrateOp).ConfigureAwait(false);

            taskRunner.RunMigrateDbTask(dbContext.GetType(), migrateOp.Id);
        }
    }
}
