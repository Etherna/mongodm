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

using Etherna.MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    public interface IReferenceModelMapsSchemaBuilder<TModel>
    {
        // Methods.
        /// <summary>
        /// Add a fallback serializer invoked in case of undefined schema id
        /// </summary>
        /// <param name="fallbackSerializer">Fallback serializer</param>
        /// <returns>This same model schema</returns>
        IReferenceModelMapsSchemaBuilder<TModel> AddFallbackCustomSerializer(
            IBsonSerializer<TModel> fallbackSerializer);

        /// <summary>
        /// Register a secondary model map
        /// </summary>
        /// <param name="id">The map Id</param>
        /// <param name="baseModelMapId">Id of the base model map for this model map</param>
        /// <param name="modelMapInitializer">The model map inizializer</param>
        /// <returns>This same model schema configuration</returns>
        IReferenceModelMapsSchemaBuilder<TModel> AddSecondaryMap(
            string id,
            string? baseModelMapId = null,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null);

        /// <summary>
        /// Register a secondary model map
        /// </summary>
        /// <param name="modelMap">The model map</param>
        /// <returns>This same model schema configuration</returns>
        IReferenceModelMapsSchemaBuilder<TModel> AddSecondaryMap(
            ModelMap<TModel> modelMap);
    }
}
