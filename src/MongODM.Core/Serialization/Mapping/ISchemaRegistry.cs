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

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongODM.Core.Serialization.Mapping.Schemas;
using Etherna.MongODM.Core.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    /// <summary>
    /// Interface for <see cref="SchemaRegistry"/> implementation.
    /// </summary>
    public interface ISchemaRegistry : IDbContextInitializable, IFreezableConfig
    {
        // Properties.
        Dictionary<string, IMemberMap> MemberMapsDictionary { get; }

        /// <summary>
        /// All registered schemas, indexed by model type
        /// </summary>
        IReadOnlyDictionary<Type, ISchema> Schemas { get; }

        // Methods.
        /// <summary>
        /// Register a new schema based on custom serializer
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="customSerializer">Custom serializer</param>
        /// <returns>The new schema</returns>
        ICustomSerializerSchemaBuilder<TModel> AddCustomSerializerSchema<TModel>(
            IBsonSerializer<TModel> customSerializer)
            where TModel : class;

        /// <summary>
        /// Register a new schema based on model map
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="activeModelMapId">The active model map Id</param>
        /// <param name="activeModelMapInitializer">The active model map inizializer</param>
        /// <param name="customSerializer">Replace default serializer with a custom</param>
        /// <returns>The new schema</returns>
        IModelSchemaBuilder<TModel> AddModelSchema<TModel>(
            string activeModelMapId,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null)
            where TModel : class;

        /// <summary>
        /// Register a new schema based on model map
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="activeModelMap">The active model map</param>
        /// <returns>The new schema</returns>
        IModelSchemaBuilder<TModel> AddModelSchema<TModel>(
            ModelMap<TModel> activeModelMap)
            where TModel : class;

        /// <summary>
        /// Get active class map from schemas, or create a default classMap for model type
        /// </summary>
        /// <returns>The active model map</returns>
        BsonClassMap GetActiveClassMap(Type modelType);

        /// <summary>
        /// Return bson element for represent a model map id
        /// </summary>
        /// <param name="modelType">The model type</param>
        /// <returns>The model map id bson element</returns>
        BsonElement GetActiveModelMapIdBsonElement(Type modelType);

        /// <summary>
        /// Get all member maps that points to a specific member definition
        /// </summary>
        /// <param name="memberInfo">The member definition</param>
        /// <returns>The list of member maps</returns>
        IEnumerable<IMemberMap> GetMemberMapsFromMemberInfo(MemberInfo memberInfo);

        /// <summary>
        /// Get a registered model schema for a given model type
        /// </summary>
        /// <param name="modelType">The model type</param>
        /// <returns>The registered model schema</returns>
        IModelSchema GetModelSchema(Type modelType);

        /// <summary>
        /// Try to get a registered model schema for a given model type
        /// </summary>
        /// <param name="modelType">The model type</param>
        /// <param name="modelSchema">Output model schema, if exists</param>
        /// <returns>Operation result</returns>
        bool TryGetModelSchema(Type modelType, out IModelSchema? modelSchema);
    }
}