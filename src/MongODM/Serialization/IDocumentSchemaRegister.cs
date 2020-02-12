using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Digicando.MongODM.Serialization
{
    /// <summary>
    /// Interface for <see cref="DocumentSchemaRegister"/> implementation.
    /// </summary>
    public interface IDocumentSchemaRegister
    {
        bool IsFrozen { get; }
        bool IsInitialized { get; }
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
        /// Call before everything else.
        /// </summary>
        /// <param name="dbContext">Instance of <see cref="DbContext"/></param>
        void Initialize(IDbContext dbContext);

        /// <summary>
        /// Register a new model schema
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="fromVersion">The first schema implementing version</param>
        /// <param name="initCustomSerializer">Custom serializer initializer</param>
        /// <param name="modelMigrationAsync">Model migration method</param>
        void RegisterModelSchema<TModel>(
            DocumentVersion fromVersion,
            Func<IBsonSerializer<TModel>> initCustomSerializer = null,
            Func<TModel, DocumentVersion, Task<TModel>> modelMigrationAsync = null)
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
            DocumentVersion fromVersion,
            Action<BsonClassMap<TModel>> classMapInitializer,
            Func<IBsonSerializer<TModel>> initCustomSerializer = null,
            Func<TModel, DocumentVersion, Task<TModel>> modelMigrationAsync = null)
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
            DocumentVersion fromVersion,
            BsonClassMap<TModel> classMap,
            Func<IBsonSerializer<TModel>> initCustomSerializer = null,
            Func<TModel, DocumentVersion, Task<TModel>> modelMigrationAsync = null)
            where TModel : class;
    }
}