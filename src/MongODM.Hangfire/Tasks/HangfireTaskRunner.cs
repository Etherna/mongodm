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

using Etherna.ExecContext.AsyncLocal;
using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.Tasks;
using Etherna.MongODM.HF.Filters;
using Hangfire;
using Hangfire.States;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.HF.Tasks
{
    public class HangfireTaskRunner : ITaskRunner
    {
        // Fields.
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly MongODMOptions mongODMOptions;

        // Constructors.
        public HangfireTaskRunner(
            IBackgroundJobClient backgroundJobClient,
            MongODMOptions mongODMOptions)
        {
            this.backgroundJobClient = backgroundJobClient;
            this.mongODMOptions = mongODMOptions;

            // Add a default execution context running with any Hangfire task.
            // Added because with asyncronous task, unrelated to requestes, there is no an alternative context to use with MongODM.
            GlobalJobFilters.Filters.Add(new AsyncLocalContextHangfireFilter(AsyncLocalContext.Instance));
        }

        // Methods.
        public void RunMigrateDbTask(Type dbContextType, string dbMigrationOpId) =>
            backgroundJobClient.Create<MigrateDbContextTaskFacade>(
                task => task.RunAsync(dbContextType, dbMigrationOpId, null!),
                new EnqueuedState(mongODMOptions.DbMaintenanceQueueName));

        public void RunUpdateDocDependenciesTask(Type dbContextType, Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId) =>
            backgroundJobClient.Create<UpdateDocDependenciesTaskFacade>(
                task => task.RunAsync(dbContextType, modelType, keyType, idPaths, modelId),
                new EnqueuedState(mongODMOptions.DbMaintenanceQueueName));
    }
}
