﻿// Copyright 2020-present Etherna SA
// This file is part of MongODM.
// 
// MongODM is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// MongODM is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with MongODM.
// If not, see <https://www.gnu.org/licenses/>.

using Etherna.MongODM.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Etherna.MongODM.HF.Tasks
{
    internal sealed class UpdateDocDependenciesTaskFacade
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
                nameof(UpdateDocDependenciesTask.RunAsync), BindingFlags.Public | BindingFlags.Instance)!
                .MakeGenericMethod(
                    dbContextType);

            return (Task)method.Invoke(task, new object[]
            {
                referenceRepositoryName,
                modelId,
                idMemberMapIdentifiers
            })!;
        }
    }
}
