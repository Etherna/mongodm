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

using Etherna.MongODM.ProxyModels;

namespace Etherna.MongODM.Utility
{
    /// <summary>
    /// Interface for <see cref="DbMaintainer"/> implementation.
    /// </summary>
    public interface IDbMaintainer : IDbContextInitializable
    {
        // Methods.
        /// <summary>
        /// Method to invoke when an auditable model is changed.
        /// </summary>
        /// <typeparam name="TKey">The model type</typeparam>
        /// <param name="auditableModel">The changed model</param>
        /// <param name="modelId">The model id</param>
        void OnUpdatedModel<TKey>(IAuditable auditableModel, TKey modelId);
    }
}