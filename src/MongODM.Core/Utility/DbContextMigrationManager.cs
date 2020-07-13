using Etherna.MongODM.Extensions;
using Etherna.MongODM.Operations;
using Etherna.MongODM.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etherna.MongODM.Utility
{
    public class DbContextMigrationManager : IDbContextMigrationManager, IDbContextInitializable
    {
        // Fields.
        private IDbContext dbContext = default!;
        private readonly ITaskRunner taskRunner;

        // Constructor and initialization.
        public DbContextMigrationManager(ITaskRunner taskRunner)
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
        public async Task<List<MigrateOperation>> GetLastMigrationsAsync(int page, int take) =>
            await dbContext.DbOperations.QueryElementsAsync(elements =>
                elements.OfType<MigrateOperation>()
                        .PaginateDescending(r => r.CreationDateTime, page, take)
                        .ToListAsync()).ConfigureAwait(false);

        public async Task<MigrateOperation> GetMigrationAsync(string migrateOperationId)
        {
            if (migrateOperationId is null)
                throw new ArgumentNullException(nameof(migrateOperationId));

            var migrateOp = await dbContext.DbOperations.QueryElementsAsync(elements =>
                elements.OfType<MigrateOperation>()
                        .Where(op => op.Id == migrateOperationId)
                        .FirstAsync()).ConfigureAwait(false);

            return migrateOp;
        }

        public async Task<MigrateOperation?> IsMigrationRunningAsync()
        {
            var migrateOp = await dbContext.DbOperations.QueryElementsAsync(elements =>
                elements.OfType<MigrateOperation>()
                        .Where(op => op.DbContextName == dbContext.Identifier)
                        .Where(op => op.CurrentStatus == MigrateOperation.Status.Running)
                        .FirstOrDefaultAsync()).ConfigureAwait(false);

            return migrateOp;
        }

        public async Task StartDbContextMigrationAsync(string authorId)
        {
            var migrateOp = new MigrateOperation(dbContext, authorId);
            await dbContext.DbOperations.CreateAsync(migrateOp).ConfigureAwait(false);

            taskRunner.RunMigrateDbContextTask(dbContext.GetType(), migrateOp.Id);
        }
    }
}
