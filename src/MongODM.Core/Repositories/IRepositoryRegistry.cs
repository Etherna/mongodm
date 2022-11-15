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
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Repositories
{
    public interface IRepositoryRegistry : IDbContextInitializable
    {
        // Properties.
        IEnumerable<IRepository> Repositories { get; }

        // Methods.
        /// <summary>
        /// Get repository that have a specific entity model type as base
        /// </summary>
        /// <typeparam name="TModel">Base model type to search</typeparam>
        /// <typeparam name="TKey">Key type of base model</typeparam>
        /// <returns>Found repository</returns>
        IRepository<TModel, TKey> GetRepositoryByBaseModelType<TModel, TKey>()
            where TModel : class, IEntityModel<TKey>;

        /// <summary>
        /// Get repository that can handle a specific entity model type
        /// </summary>
        /// <param name="modelType">Model type to search</param>
        /// <returns>Entity model handling repository</returns>
        IRepository GetRepositoryByHandledModelType(Type modelType);

        /// <summary>
        /// Try to get repository that can handle a specific entity model type
        /// </summary>
        /// <param name="modelType">Model type to search</param>
        /// <returns>Entity model handling repository. Null if doesn't exist</returns>
        IRepository? TryGetRepositoryByHandledModelType(Type modelType);
    }
}