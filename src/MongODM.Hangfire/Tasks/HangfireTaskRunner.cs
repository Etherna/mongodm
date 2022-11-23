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

using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.Tasks;
using Hangfire;
using Hangfire.States;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.HF.Tasks
{
    public sealed class HangfireTaskRunner : ITaskRunner, ITaskRunnerBuilder
    {
        // Fields.
        private readonly IBackgroundJobClient backgroundJobClient;
        private MongODMOptions mongODMOptions = default!;

        // Constructors.
        public HangfireTaskRunner(
            IBackgroundJobClient backgroundJobClient)
        {
            this.backgroundJobClient = backgroundJobClient;
        }

        // Methods.
        public void RunMigrateDbTask(Type dbContextType, string dbMigrationOpId) =>
            backgroundJobClient.Create<MigrateDbContextTaskFacade>(
                task => task.RunAsync(dbContextType, dbMigrationOpId, null!),
                new EnqueuedState(mongODMOptions.DbMaintenanceQueueName));

        public void RunUpdateDocDependenciesTask(
            Type dbContextType,
            Type referenceRepositoryType,
            IEnumerable<string> idMemberMapIdentifiers,
            object modelId) =>
            backgroundJobClient.Create<UpdateDocDependenciesTaskFacade>(
                task => task.RunAsync(
                    dbContextType,
                    referenceRepositoryType,
                    idMemberMapIdentifiers,
                    modelId),
                new EnqueuedState(mongODMOptions.DbMaintenanceQueueName));

        // Explicit methods.
        void ITaskRunnerBuilder.SetMongODMOptions(MongODMOptions options) =>
            mongODMOptions = options;
    }
}
