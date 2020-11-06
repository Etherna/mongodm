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

using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    public interface IModelMapsSchema : ISchema
    {
        // Properties.
        ModelMap ActiveMap { get; }
        IDictionary<string, ModelMap> AllMapsDictionary { get; }
        IEnumerable<ModelMap> SecondaryMaps { get; }
    }

    public interface IModelMapsSchema<TModel> : IModelMapsSchema
        where TModel : class
    {
        // Properties.
        IBsonSerializer<TModel>? FallbackSerializer { get; }

        // Methods.
        /// <summary>
        /// Add a fallback serializer invoked in case of undefined schema id
        /// </summary>
        /// <param name="fallbackSerializer">Fallback serializer</param>
        /// <returns>This same model schema</returns>
        IModelMapsSchema<TModel> AddFallbackCustomSerializer(
            IBsonSerializer<TModel> fallbackSerializer);

        /// <summary>
        /// Register a secondary model map
        /// </summary>
        /// <param name="id">The map Id</param>
        /// <param name="baseModelMapId">Id of the base model map for this model map</param>
        /// <param name="modelMapInitializer">The model map inizializer</param>
        /// <param name="customSerializer">Custom serializer</param>
        /// <returns>This same model schema configuration</returns>
        IModelMapsSchema<TModel> AddSecondaryMap(
            string id,
            string? baseModelMapId = null,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null);

        /// <summary>
        /// Register a secondary model map
        /// </summary>
        /// <param name="modelMap">The model map</param>
        /// <returns>This same model schema configuration</returns>
        IModelMapsSchema<TModel> AddSecondaryMap(
            ModelMap<TModel> modelMap);
    }
}