using Etherna.ExecContext.AsyncLocal;
using Etherna.MongODM.HF.Filters;
using Etherna.MongODM.Tasks;
using Hangfire;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.HF.Tasks
{
    public class HangfireTaskRunner : ITaskRunner
    {
        // Fields.
        private readonly IBackgroundJobClient backgroundJobClient;

        // Constructors.
        public HangfireTaskRunner(
            IBackgroundJobClient backgroundJobClient)
        {
            this.backgroundJobClient = backgroundJobClient;

            // Add a default execution context running with any Hangfire task.
            // Added because with asyncronous task, unrelated to requestes, there is no an alternative context to use with MongODM.
            GlobalJobFilters.Filters.Add(new AsyncLocalContextHangfireFilter(AsyncLocalContext.Instance));
        }

        // Methods.
        public void RunMigrateDbTask(Type dbContextType, string dbMigrationOpId) =>
            backgroundJobClient.Enqueue<MigrateDbContextTaskFacade>(
                task => task.RunAsync(dbContextType, dbMigrationOpId, null!));

        public void RunUpdateDocDependenciesTask(Type dbContextType, Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId) =>
            backgroundJobClient.Enqueue<UpdateDocDependenciesTaskFacade>(
                task => task.RunAsync(dbContextType, modelType, keyType, idPaths, modelId));
    }
}
