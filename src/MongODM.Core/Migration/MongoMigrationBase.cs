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

using Etherna.MongODM.Core.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Migration
{
    public abstract class MongoMigrationBase
    {
        // Constructors.
        public MongoMigrationBase(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        // Properties.
        public string Id { get; }
        public abstract ICollectionRepository SourceCollection { get; }

        // Methods.
        /// <summary>
        /// Perform migration with optional updating callback
        /// </summary>
        /// <param name="callbackEveryDocuments">Interval of processed documents between callback invokations. 0 if ignore callback</param>
        /// <param name="callbackAsync">The async callback function. Parameter is number of processed documents</param>
        /// <returns>The migration result</returns>
        public abstract Task<MigrationResult> MigrateAsync(int callbackEveryDocuments = 0, Func<long, Task>? callbackAsync = null, CancellationToken cancellationToken = default);
    }
}
