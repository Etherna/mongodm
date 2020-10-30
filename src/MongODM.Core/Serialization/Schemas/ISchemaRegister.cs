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
using System.Reflection;

namespace Etherna.MongODM.Core.Serialization.Schemas
{
    /// <summary>
    /// Interface for <see cref="SchemaRegister"/> implementation.
    /// </summary>
    public interface ISchemaRegister : IDbContextInitializable, IFreezableConfig
    {
        // Methods.
        /// <summary>
        /// Register a new schema based on custom serializer
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="customSerializer">Custom serializer</param>
        /// <param name="requireCollectionMigration">Migrate full collection on db migration</param>
        /// <returns>Configuration of schema</returns>
        ICustomSerializerSchemaConfig<TModel> AddCustomSerializerSchema<TModel>(
            IBsonSerializer<TModel> customSerializer,
            bool requireCollectionMigration = false)
            where TModel : class;

        /// <summary>
        /// Register a new schema based on model map
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="id">The schema Id</param>
        /// <param name="activeModelMapInitializer">The active model map inizializer</param>
        /// <param name="customSerializer">Replace default serializer with a custom</param>
        /// <param name="requireCollectionMigration">Migrate full collection on db migration</param>
        /// <returns>Configuration of schema</returns>
        IModelMapSchemaConfig<TModel> AddModelMapSchema<TModel>(
            string id,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null,
            bool requireCollectionMigration = false)
            where TModel : class;

        /// <summary>
        /// Register a new schema based on model map
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="activeModelSchema">The active model map schema</param>
        /// <param name="requireCollectionMigration">Migrate full collection on db migration</param>
        /// <returns>Configuration of schema</returns>
        IModelMapSchemaConfig<TModel> AddModelMapSchema<TModel>(
            ModelMapSchema<TModel> activeModelSchema,
            bool requireCollectionMigration = false)
            where TModel : class;

        IEnumerable<SchemaMemberMap> GetMemberDependencies(MemberInfo memberInfo);

        IEnumerable<SchemaMemberMap> GetModelDependencies(Type modelType);

        IEnumerable<SchemaMemberMap> GetModelEntityReferencesIds(Type modelType);
    }
}