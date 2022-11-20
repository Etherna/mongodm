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

using Etherna.MongoDB.Driver;
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Etherna.MongODM.Core.Utility
{
    public class DbMaintainer : IDbMaintainer
    {
        // Fields.
        private IDbContext dbContext = default!;
        private ILogger logger = default!;
        private readonly ITaskRunner taskRunner;

        // Constructors and initialization.
        public DbMaintainer(ITaskRunner taskRunner)
        {
            this.taskRunner = taskRunner;
        }

        public void Initialize(IDbContext dbContext, ILogger logger)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");

            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            IsInitialized = true;

            this.logger.DbMaintainerInitialized(dbContext.Options.DbName);
        }

        // Properties.
        public bool IsInitialized { get; private set; }

        // Methods.
        public void OnUpdatedModel<TKey>(IAuditable updatedModel, TKey modelId)
        {
            if (updatedModel is null)
                throw new ArgumentNullException(nameof(updatedModel));
            if (modelId is null)
                throw new ArgumentNullException(nameof(modelId));

            // Find all possibly involved member maps with changes. Select only referenced members
            var updatedMembersInfo = updatedModel.ChangedMembers;
            var referenceMemberMaps = updatedMembersInfo.SelectMany(updatedMemberInfo => dbContext.SchemaRegistry.GetMemberMapsFromMemberInfo(updatedMemberInfo))
                                                        .Where(memberDependency => memberDependency.IsEntityReferenceMember);

            // Group by repository of origin root model
            foreach (var referenceMemberMapsGroupedByOriginRepo in referenceMemberMaps.GroupBy(
                memberMap => dbContext.RepositoryRegistry.GetRepositoryByHandledModelType(memberMap.RootModelMap.ModelType)))
            {
                // Extract only id member maps of referenced entities.
                var idMemberMapIdentifiers = referenceMemberMapsGroupedByOriginRepo
                    .Select(memberMap =>
                    {
                        var ownerEntityModelMap = memberMap.OwnerEntityModelMap;
                        var idMemberMap = ownerEntityModelMap!.IdMemberMap!;
                        return idMemberMap.Id;
                    })
                    .Distinct();

                // Enqueue call of background job.
                taskRunner.RunUpdateDocDependenciesTask(dbContext.GetType(), referenceMemberMapsGroupedByOriginRepo.Key.ModelType, typeof(TKey), idMemberMapIdentifiers, modelId);
            }
        }
    }
}
