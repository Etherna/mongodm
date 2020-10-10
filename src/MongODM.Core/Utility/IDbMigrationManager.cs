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

using Etherna.MongODM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etherna.MongODM.Utility
{
    public interface IDbMigrationManager : IDbContextInitializable
    {
        Task<DbMigrationOperation?> IsMigrationRunningAsync();

        Task<List<DbMigrationOperation>> GetLastMigrationsAsync(int page, int take);

        Task<DbMigrationOperation> GetMigrationAsync(string migrateOperationId);

        /// <summary>
        /// Start a db context migration process.
        /// </summary>
        /// <param name="authorId">Id of user requiring the migration</param>
        Task StartDbContextMigrationAsync(string authorId);
    }
}