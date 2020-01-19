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
        }

        // Methods.
        public void RunUpdateDocDependenciesTask(Type dbContextType, Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId) =>
            backgroundJobClient.Enqueue<UpdateDocDependenciesTaskFacade>(
                task => task.RunAsync(dbContextType, modelType, keyType, idPaths, modelId));
    }
}
