using System;
using System.Threading.Tasks;

namespace Etherna.MongODM.Tasks
{
    public class MigrateDbTask : IMigrateDbTask
    {
        // Fields.
        private readonly IServiceProvider serviceProvider;

        // Constructors.
        public MigrateDbTask(
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        // Methods.
        public async Task RunAsync<TDbContext>()
            where TDbContext : class, IDbContext
        {
            var dbContext = (TDbContext)serviceProvider.GetService(typeof(TDbContext));

            // Migrate collections.
            foreach (var migration in dbContext.MigrationTaskList)
                await migration.MigrateAsync(cancellationToken).ConfigureAwait(false);

            // Build indexes.
            foreach (var repository in dbContext.RepositoryRegister.ModelCollectionRepositoryMap.Values)
                await repository.BuildIndexesAsync(dbContext.DocumentSchemaRegister, cancellationToken).ConfigureAwait(false);
        }
    }
}
