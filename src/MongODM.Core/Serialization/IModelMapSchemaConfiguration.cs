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

using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Serialization
{
    public interface IModelMapSchemaConfiguration<TModel> : ISchemaConfiguration
        where TModel : class
    {
        // Properties.
        ModelMapSchema ActiveSchema { get; }
        IBsonSerializer? FallbackSerializer { get; }
        IEnumerable<ModelMapSchema> SecondarySchemas { get; }

        // Methods.
        /// <summary>
        /// Add a fallback serializer invoked in case of undefined schema id
        /// </summary>
        /// <param name="fallbackSerializer">Fallback serializer</param>
        /// <returns>This same model schema configuration</returns>
        IModelMapSchemaConfiguration<TModel> AddFallbackCustomSerializer(
            IBsonSerializer<TModel> fallbackSerializer);

        /// <summary>
        /// Register a secondary model schema
        /// </summary>
        /// <param name="id">The schema Id</param>
        /// <param name="modelMapInitializer">The model map inizializer</param>
        /// <param name="customSerializer">Custom serializer</param>
        /// <returns>This same model schema configuration</returns>
        IModelMapSchemaConfiguration<TModel> AddSecondarySchema(
            string id,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null);

        /// <summary>
        /// Register a secondary model schema
        /// </summary>
        /// <param name="modelMapSchema">The model schema</param>
        /// <returns>This same model schema configuration</returns>
        IModelMapSchemaConfiguration<TModel> AddSecondarySchema(
            ModelMapSchema<TModel> modelMapSchema);
    }
}