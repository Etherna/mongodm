//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

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
        public Task RunAsync(Type dbContextType, string dbMigrationOpId, PerformContext context)
        {
            var method = typeof(MigrateDbContextTask).GetMethod(
                nameof(MigrateDbContextTask.RunAsync), BindingFlags.Public | BindingFlags.Instance)
                .MakeGenericMethod(dbContextType);

            return (Task)method.Invoke(task, new object[] { dbMigrationOpId, context.BackgroundJob.Id });
        }
    }
}
