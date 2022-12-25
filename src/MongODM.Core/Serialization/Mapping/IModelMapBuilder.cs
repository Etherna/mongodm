﻿//   Copyright 2020-present Etherna Sagl
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

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public interface IModelMapBuilder<TModel>
    {
        // Methods.
        /// <summary>
        /// Add a fallback serializer invoked in case of unrecognized schema id
        /// </summary>
        /// <param name="fallbackSerializer">Fallback serializer</param>
        /// <returns>This same model map</returns>
        IModelMapBuilder<TModel> AddFallbackCustomSerializerMap(
            IBsonSerializer<TModel> fallbackSerializer);

        /// <summary>
        /// Add a fallback model map invoked in case of unrecognized schema id, and absence of fallback serializer
        /// </summary>
        /// <param name="modelMapSchemaInitializer">The model map inizializer</param>
        /// <param name="baseModelMapSchemaId">Id of the base model map for this model map</param>
        /// <returns>This same model map</returns>
        IModelMapBuilder<TModel> AddFallbackModelMapSchema(
            Action<BsonClassMap<TModel>>? modelMapSchemaInitializer = null,
            string? baseModelMapSchemaId = null);

        /// <summary>
        /// Register a secondary model map schema
        /// </summary>
        /// <param name="id">The map Id</param>
        /// <param name="modelMapSchemaInitializer">The model map schema inizializer</param>
        /// <param name="baseModelMapSchemaId">Id of the base model map schema for this model map schema</param>
        /// <returns>This same model map</returns>
        IModelMapBuilder<TModel> AddSecondaryModelMapSchema(
            string id,
            Action<BsonClassMap<TModel>>? modelMapSchemaInitializer = null,
            string? baseModelMapSchemaId = null);
    }
}