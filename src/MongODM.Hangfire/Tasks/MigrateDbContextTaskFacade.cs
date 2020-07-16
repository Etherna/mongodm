using Etherna.MongODM.Tasks;
using Hangfire;
using Hangfire.Server;
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
        public Task RunAsync(Type dbContextType, string dbMigrationOpId, PerformingContext context)
        {
            var method = typeof(MigrateDbContextTask).GetMethod(
                nameof(MigrateDbContextTask.RunAsync), BindingFlags.Public | BindingFlags.Instance)
                .MakeGenericMethod(dbContextType);

            return (Task)method.Invoke(task, new object[] { dbMigrationOpId, context.BackgroundJob.Id });
        }
    }
}
