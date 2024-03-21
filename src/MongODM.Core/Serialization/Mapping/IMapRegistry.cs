// Copyright 2020-present Etherna SA
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongODM.Core.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    /// <summary>
    /// Interface for <see cref="MapRegistry"/> implementation.
    /// </summary>
    public interface IMapRegistry : IDbContextInitializable, IFreezableConfig
    {
        // Properties.
        /// <summary>
        /// All registered maps, indexed by model type
        /// </summary>
        IReadOnlyDictionary<Type, IMap> MapsByModelType { get; }

        IReadOnlyDictionary<string, IMemberMap> MemberMapsById { get; }

        // Methods.
        /// <summary>
        /// Register a new map based on custom serializer
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="customSerializer">Custom serializer</param>
        /// <returns>The new schema</returns>
        ICustomSerializerMapBuilder<TModel> AddCustomSerializerMap<TModel>(
            IBsonSerializer<TModel> customSerializer)
            where TModel : class;

        /// <summary>
        /// Register a new schema based on model map
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="activeModelMapSchemaId">The active model map schema Id</param>
        /// <param name="activeModelMapSchemaInitializer">The active model map schema inizializer</param>
        /// <param name="customSerializer">Replace default serializer with a custom</param>
        /// <returns>The new model map</returns>
        IModelMapBuilder<TModel> AddModelMap<TModel>(
            string activeModelMapSchemaId,
            Action<BsonClassMap<TModel>>? activeModelMapSchemaInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null)
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
        /// <returns>List of member maps</returns>
        IEnumerable<IMemberMap> GetMemberMapsFromMemberInfo(MemberInfo memberInfo);

        IEnumerable<IMemberMap> GetMemberMapsWithSameElementPath(IMemberMap memberMap);

        /// <summary>
        /// Get a registered model map for a given model type
        /// </summary>
        /// <param name="modelType">The model type</param>
        /// <returns>The registered model schema</returns>
        IModelMap GetModelMap(Type modelType);

        /// <summary>
        /// Try to get a registered model map for a given model type
        /// </summary>
        /// <param name="modelType">The model type</param>
        /// <param name="modelMap">Output model map, if exists</param>
        /// <returns>Operation result</returns>
        bool TryGetModelMap(Type modelType, out IModelMap? modelMap);
    }
}