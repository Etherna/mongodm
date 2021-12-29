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
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Utility
{
    /// <summary>
    /// Interface for <see cref="DbCache"/> implementation.
    /// </summary>
    public interface IDbCache : IDbContextInitializable
    {
        // Properties.
        /// <summary>
        /// List of current cached models, indexed by Id.
        /// </summary>
        IReadOnlyDictionary<object, IEntityModel> LoadedModels { get; } // (id -> model)

        // Methods.
        /// <summary>
        /// Add a new model in cache.
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="id">The model Id</param>
        /// <param name="model">The model</param>
        void AddModel<TModel>(object id, TModel model)
            where TModel : class, IEntityModel;

        /// <summary>
        /// Clear current cache archive.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Remove a model from cache.
        /// </summary>
        /// <param name="id">The model Id</param>
        void RemoveModel(object id);
    }
}