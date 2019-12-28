using Digicando.MongoDM.Tasks;
using Hangfire;
using System;
using System.Collections.Generic;

namespace Digicando.MongoDM.HF.Tasks
{
    public class TaskRunner : ITaskRunner
    {
        // Fields.
        private readonly IBackgroundJobClient backgroundJobClient;

        // Constructors.
        public TaskRunner(
            IBackgroundJobClient backgroundJobClient)
        {
            this.backgroundJobClient = backgroundJobClient;
        }

        // Methods.
        public void RunUpdateDocDependenciesTask(Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId) =>
            backgroundJobClient.Enqueue<UpdateDocDependenciesTaskFacade>(
                task => task.RunAsync(modelType, keyType, idPaths, modelId));
    }
}
