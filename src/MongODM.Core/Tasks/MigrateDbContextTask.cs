using Etherna.MongODM.Operations;
using System;
using System.Threading.Tasks;

namespace Etherna.MongODM.Tasks
{
    public class MigrateDbContextTask : IMigrateDbContextTask
    {
        // Fields.
        private readonly IServiceProvider serviceProvider;

        // Constructors.
        public MigrateDbContextTask(
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        // Methods.
        public async Task RunAsync<TDbContext>(string migrateOpId)
            where TDbContext : class, IDbContext
        {
            var dbContext = (TDbContext)serviceProvider.GetService(typeof(TDbContext));
            var migrateOp = (MigrateOperation)await dbContext.DbOperations.FindOneAsync(migrateOpId).ConfigureAwait(false);

            // Start migrate operation.
            migrateOp.TaskStarted();
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Migrate collections.
            foreach (var migration in dbContext.MigrationTaskList)
                await migration.MigrateAsync(cancellationToken).ConfigureAwait(false);

            // Build indexes.
            foreach (var repository in dbContext.RepositoryRegister.ModelCollectionRepositoryMap.Values)
                await repository.BuildIndexesAsync(dbContext.DocumentSchemaRegister, cancellationToken).ConfigureAwait(false);
        }
    }
}
