using Digicando.MongoDM.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Digicando.MongoDM.HF.Tasks
{
    public class TaskRunner : ITaskRunner
    {
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IServiceProvider serviceProvider;

        public TaskRunner(
            IBackgroundJobClient backgroundJobClient,
            IServiceProvider serviceProvider)
        {
            this.backgroundJobClient = backgroundJobClient;
            this.serviceProvider = serviceProvider;
        }

        public string Enqueue<T>(Expression<Func<T, Task>> methodCall) =>
            backgroundJobClient.Enqueue(methodCall);

        public void RunUpdateDocDependenciesTask(Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId) =>
            backgroundJobClient.Enqueue(() => RunUpdateDocDependenciesTaskHelper(modelType, keyType, idPaths, modelId));

        // Helpers.
        [Queue(Queues.DB_MAINTENANCE)]
        private Task RunUpdateDocDependenciesTaskHelper(
            Type modelType,
            Type keyType,
            IEnumerable<string> idPaths,
            object modelId)
        {
            var task = serviceProvider.GetService<IUpdateDocDependenciesTask>();
            return typeof(UpdateDocDependenciesTask).GetMethod(nameof(UpdateDocDependenciesTask.RunAsync),
                                                               BindingFlags.Public | BindingFlags.Instance)
                .MakeGenericMethod(modelType, keyType)
                .Invoke(task, new object[] { idPaths, modelId }) as Task;
        }
    }
}
