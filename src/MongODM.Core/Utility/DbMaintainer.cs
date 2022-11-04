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
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Tasks;
using System;
using System.Linq;

namespace Etherna.MongODM.Core.Utility
{
    public class DbMaintainer : IDbMaintainer
    {
        // Fields.
        private IDbContext dbContext = default!;
        private readonly ITaskRunner taskRunner;

        // Constructors and initialization.
        public DbMaintainer(ITaskRunner taskRunner)
        {
            this.taskRunner = taskRunner;
        }

        public void Initialize(IDbContext dbContext)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");

            this.dbContext = dbContext;

            IsInitialized = true;
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

            // Find all possible coinvolted member maps with changes. Keep only referenced members
            var updatedMembers = updatedModel.ChangedMembers;
            var referenceMemberMaps = updatedMembers.SelectMany(memberInfo => dbContext.SchemaRegistry.GetMemberDependenciesFromMemberInfo(memberInfo))
                                                    .Where(memberMap => memberMap.IsEntityReferenceMember);

            // Group by root model type, and select only model types related to a collections.
            foreach (var dependencyGroup in referenceMemberMaps.GroupBy(memberMap => memberMap.RootModelMap.ModelType)
                                                               .Where(group => dbContext.RepositoryRegistry.RepositoriesByModelType.ContainsKey(group.Key)))
            {
                // Extract only id paths to referenced entities.
                var idPaths = dependencyGroup
                    .Select(memberMap => string.Join(".", memberMap.MemberPathToLastEntityModelId.Select(idMember => idMember.Member.MemberInfo.Name)))
                    .Distinct();

                // Enqueue call for background job.
                taskRunner.RunUpdateDocDependenciesTask(dbContext.GetType(), dependencyGroup.Key, typeof(TKey), idPaths, modelId);
            }
        }
    }
}
