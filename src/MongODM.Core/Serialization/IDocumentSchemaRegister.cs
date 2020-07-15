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
using System.Threading.Tasks;

namespace Etherna.MongODM.Serialization
{
    /// <summary>
    /// Interface for <see cref="DocumentSchemaRegister"/> implementation.
    /// </summary>
    public interface IDocumentSchemaRegister : IDbContextInitializable
    {
        bool IsFrozen { get; }
        IEnumerable<DocumentSchema> Schemas { get; }

        // Methods.
        /// <summary>
        /// Build and freeze the register.
        /// </summary>
        void Freeze();

        IEnumerable<DocumentSchemaMemberMap> GetMemberDependencies(MemberInfo memberInfo);

        IEnumerable<DocumentSchemaMemberMap> GetModelDependencies(Type modelType);

        IEnumerable<DocumentSchemaMemberMap> GetModelEntityReferencesIds(Type modelType);

        /// <summary>
        /// Register a new model schema
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="fromVersion">The first schema implementing version</param>
        /// <param name="initCustomSerializer">Custom serializer initializer</param>
        /// <param name="modelMigrationAsync">Model migration method</param>
        void RegisterModelSchema<TModel>(
            SemanticVersion fromVersion,
            Func<IBsonSerializer<TModel>>? initCustomSerializer = null,
            Func<TModel, SemanticVersion?, Task<TModel>>? modelMigrationAsync = null)
            where TModel : class;

        /// <summary>
        /// Register a new model schema
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="fromVersion">The first schema implementing version</param>
        /// <param name="classMapInitializer">The class map inizializer</param>
        /// <param name="initCustomSerializer">Custom serializer initializer</param>
        /// <param name="modelMigrationAsync">Model migration method</param>
        void RegisterModelSchema<TModel>(
            SemanticVersion fromVersion,
            Action<BsonClassMap<TModel>> classMapInitializer,
            Func<IBsonSerializer<TModel>>? initCustomSerializer = null,
            Func<TModel, SemanticVersion?, Task<TModel>>? modelMigrationAsync = null)
            where TModel : class;

        /// <summary>
        /// Register a new model schema
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="fromVersion">The first schema implementing version</param>
        /// <param name="classMap">The class map</param>
        /// <param name="initCustomSerializer">Custom serializer initializer</param>
        /// <param name="modelMigrationAsync">Model migration method</param>
        void RegisterModelSchema<TModel>(
            SemanticVersion fromVersion,
            BsonClassMap<TModel> classMap,
            Func<IBsonSerializer<TModel>>? initCustomSerializer = null,
            Func<TModel, SemanticVersion?, Task<TModel>>? modelMigrationAsync = null)
            where TModel : class;
    }
}