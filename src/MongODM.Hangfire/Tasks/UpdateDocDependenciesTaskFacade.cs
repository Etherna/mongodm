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

using Etherna.MongODM.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Etherna.MongODM.HF.Tasks
{
    class UpdateDocDependenciesTaskFacade
    {
        // Fields.
        private readonly IUpdateDocDependenciesTask task;

        // Constructors.
        public UpdateDocDependenciesTaskFacade(IUpdateDocDependenciesTask task)
        {
            this.task = task;
        }

        // Methods.
        public Task RunAsync(
            Type dbContextType,
            string referenceRepositoryName,
            object modelId,
            IEnumerable<string> idMemberMapIdentifiers)
        {
            var method = typeof(UpdateDocDependenciesTask).GetMethod(
                nameof(UpdateDocDependenciesTask.RunAsync), BindingFlags.Public | BindingFlags.Instance)
                .MakeGenericMethod(
                    dbContextType);

            return (Task)method.Invoke(task, new object[]
            {
                referenceRepositoryName,
                modelId,
                idMemberMapIdentifiers
            });
        }
    }
}
