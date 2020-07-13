using Etherna.MongODM.Tasks;
using Hangfire;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Etherna.MongODM.HF.Tasks
{
    class MigrateDbContextTaskFacade
    {
        // Fields.
        private readonly IMigrateDbContextTask task;

        // Constructors.
        public MigrateDbContextTaskFacade(IMigrateDbContextTask task)
        {
            this.task = task;
        }

        // Methods.
        [Queue(Queues.DB_MAINTENANCE)]
        public Task RunAsync(Type dbContextType, string migrateOpId)
        {
            var method = typeof(MigrateDbContextTask).GetMethod(
                nameof(MigrateDbContextTask.RunAsync), BindingFlags.Public | BindingFlags.Instance)
                .MakeGenericMethod(dbContextType);

            return (Task)method.Invoke(task, new object[] { migrateOpId });
        }
    }
}
