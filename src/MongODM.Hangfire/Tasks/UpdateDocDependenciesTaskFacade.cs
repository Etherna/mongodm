using Etherna.MongODM.Tasks;
using Hangfire;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

namespace Etherna.MongODM.HF.Tasks
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "It is instantiated by Hangfire")]
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
        public Task RunAsync(Type dbContextType, Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId)
        {
            var method = typeof(UpdateDocDependenciesTask).GetMethod(
                nameof(UpdateDocDependenciesTask.RunAsync), BindingFlags.Public | BindingFlags.Instance)
                .MakeGenericMethod(dbContextType, modelType, keyType);

            return (Task)method.Invoke(task, new object[] { idPaths, modelId });
        }
    }
}
