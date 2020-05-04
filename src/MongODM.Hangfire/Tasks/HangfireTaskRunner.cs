using Digicando.ExecContext.AsyncLocal;
using Digicando.MongODM.HF.Filters;
using Digicando.MongODM.Tasks;
using Hangfire;
using System;
using System.Collections.Generic;

namespace Digicando.MongODM.HF.Tasks
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
        public void RunUpdateDocDependenciesTask(Type dbContextType, Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId) =>
            backgroundJobClient.Enqueue<UpdateDocDependenciesTaskFacade>(
                task => task.RunAsync(dbContextType, modelType, keyType, idPaths, modelId));
    }
}
