using Digicando.MongoDM.Tasks;
using Hangfire;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Digicando.MongoDM.HF.Tasks
{
    class UpdateDocDependenciesTaskFacade
    {
        // Fields.
        private readonly IUpdateDocDependenciesTask task;

        // Constructors.
        public UpdateDocDependenciesTaskFacade(IUpdateDocDependenciesTask task)
        {
            this.task = task;
        }

        // Methods.
        [Queue(Queues.DB_MAINTENANCE)]
        public Task RunAsync(Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId) =>
            typeof(UpdateDocDependenciesTask).GetMethod(
                nameof(UpdateDocDependenciesTask.RunAsync), BindingFlags.Public | BindingFlags.Instance)
                .MakeGenericMethod(modelType, keyType)
                .Invoke(task, new object[] { idPaths, modelId }) as Task;
    }
}
