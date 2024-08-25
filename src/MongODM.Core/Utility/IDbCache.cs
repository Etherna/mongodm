// Copyright 2020-present Etherna SA
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