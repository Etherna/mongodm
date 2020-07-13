using Etherna.MongODM.Models.Internal;
using Etherna.MongODM.Models.Internal.MigrateOpAgg;
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
        public async Task RunAsync<TDbContext>(string migrateOpId, string taskId)
            where TDbContext : class, IDbContext
        {
            var dbContext = (TDbContext)serviceProvider.GetService(typeof(TDbContext));
            var migrateOp = (MigrateOperation)await dbContext.DbOperations.FindOneAsync(migrateOpId).ConfigureAwait(false);

            // Start migrate operation.
            migrateOp.TaskStarted(taskId);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Migrate collections.
            foreach (var migration in dbContext.MigrationTaskList)
            {
                var migrationType = migration.GetType().Name;

                //running migration
                var result = await migration.MigrateAsync(500,
                    async procDocs =>
                    {
                        migrateOp.AddLog(new MigrateExecutionLog(
                            MigrateExecutionLog.ExecutionState.Executing,
                            migration.Id,
                            migrationType,
                            procDocs));

                        await dbContext.SaveChangesAsync().ConfigureAwait(false);
                    }).ConfigureAwait(false);

                //ended migration log
                migrateOp.AddLog(new MigrateExecutionLog(
                    result.Succeded ?
                        MigrateExecutionLog.ExecutionState.Succeded :
                        MigrateExecutionLog.ExecutionState.Failed,
                    migration.Id,
                    migrationType,
                    result.MigratedDocuments));

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            // Build indexes.
            foreach (var repository in dbContext.RepositoryRegister.ModelCollectionRepositoryMap.Values)
                await repository.BuildIndexesAsync(dbContext.DocumentSchemaRegister, cancellationToken).ConfigureAwait(false);
        }
    }
}
