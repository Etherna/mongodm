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

using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Domain.Models.DbMigrationOpAgg;
using System;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Tasks
{
    public class MigrateDbContextTask : IMigrateDbContextTask
    {
        // Fields.
        private readonly IServiceProvider serviceProvider;

        // Constructors.
        public MigrateDbContextTask(
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        // Methods.
        public async Task RunAsync<TDbContext>(string dbMigrationOpId, string taskId)
            where TDbContext : class, IDbContext
        {
            var dbContext = (TDbContext)serviceProvider.GetService(typeof(TDbContext));
            var dbMigrationOp = (DbMigrationOperation)await dbContext.DbOperations.FindOneAsync(dbMigrationOpId).ConfigureAwait(false);

            // Start migrate operation.
            dbMigrationOp.TaskStarted(taskId);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Migrate documents.
            foreach (var docMigration in dbContext.DocumentMigrationList)
            {
                //running document migration
                var result = await docMigration.MigrateAsync(500,
                    async procDocs =>
                    {
                        dbMigrationOp.AddLog(new DocumentMigrationLog(
                            docMigration.SourceCollection.Name,
                            MigrationLogBase.ExecutionState.Executing,
                            procDocs));

                        await dbContext.SaveChangesAsync().ConfigureAwait(false);
                    }).ConfigureAwait(false);

                //ended document migration log
                dbMigrationOp.AddLog(new DocumentMigrationLog(
                    docMigration.SourceCollection.Name,
                    result.Succeded ?
                        MigrationLogBase.ExecutionState.Succeded :
                        MigrationLogBase.ExecutionState.Failed,
                    result.MigratedDocuments));

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            // Build indexes.
            foreach (var repository in dbContext.RepositoryRegistry.ModelCollectionRepositoryMap.Values)
            {
                dbMigrationOp.AddLog(new IndexMigrationLog(
                    repository.Name,
                    MigrationLogBase.ExecutionState.Executing));
                await dbContext.SaveChangesAsync().ConfigureAwait(false);

                await repository.BuildIndexesAsync(dbContext.SchemaRegistry).ConfigureAwait(false);

                dbMigrationOp.AddLog(new IndexMigrationLog(
                    repository.Name,
                    MigrationLogBase.ExecutionState.Succeded));
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            // Complete task.
            dbMigrationOp.TaskCompleted();
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
