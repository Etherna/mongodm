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

namespace Etherna.MongODM.Core.Serialization
{
    /// <summary>
    /// Interface for <see cref="ModelSchemaConfigurationRegister"/> implementation.
    /// </summary>
    public interface IModelSchemaConfigurationRegister : IDbContextInitializable
    {
        bool IsFrozen { get; }

        // Methods.
        /// <summary>
        /// Build and freeze the register.
        /// </summary>
        void Freeze();

        IEnumerable<ModelSchemaMemberMap> GetMemberDependencies(MemberInfo memberInfo);

        IEnumerable<ModelSchemaMemberMap> GetModelDependencies(Type modelType);

        IEnumerable<ModelSchemaMemberMap> GetModelEntityReferencesIds(Type modelType);

        /// <summary>
        /// Register a new model schema configuration
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="id">The schema Id</param>
        /// <param name="modelMapInitializer">The model map inizializer</param>
        /// <param name="customSerializer">Custom serializer</param>
        /// <param name="requireCollectionMigration">Migrate full collection on db migration</param>
        /// <returns>Configuration of model schema</returns>
        IModelSchemaConfiguration<TModel> AddModel<TModel>(
            string id,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null,
            bool requireCollectionMigration = false)
            where TModel : class;

        /// <summary>
        /// Register a new model schema configuration
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="modelSchema">The model schema</param>
        /// <param name="requireCollectionMigration">Migrate full collection on db migration</param>
        /// <returns>Configuration of model schema</returns>
        IModelSchemaConfiguration<TModel> AddModel<TModel>(
            ModelSchema<TModel> modelSchema,
            bool requireCollectionMigration = false)
            where TModel : class;
    }
}