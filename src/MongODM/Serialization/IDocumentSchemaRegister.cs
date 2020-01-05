using Digicando.MongODM.Serialization.Modifiers;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Digicando.MongODM.Serialization
{
    public interface IDocumentSchemaRegister
    {
        /// <summary>
        /// Build and freeze the register
        /// </summary>
        /// <returns>This instance</returns>
        IDocumentSchemaRegister Freeze();

        IEnumerable<DocumentSchemaMemberMap> GetMemberDependencies(MemberInfo memberInfo);

        IEnumerable<DocumentSchemaMemberMap> GetModelDependencies(Type modelType);

        IEnumerable<DocumentSchemaMemberMap> GetModelEntityReferencesIds(Type modelType);

        /// <summary>
        /// Call before everything else. Used for avoid circular dependency injection with MongoStorage
        /// </summary>
        /// <param name="dbContext">Current instance of IDBContext</param>
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
            Func<TModel, DocumentVersion, ISerializerModifierAccessor, Task<TModel>> modelMigrationAsync = null)
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
            Action<BsonClassMap<TModel>, ISerializerModifierAccessor> classMapInitializer,
            Func<IBsonSerializer<TModel>> initCustomSerializer = null,
            Func<TModel, DocumentVersion, ISerializerModifierAccessor, Task<TModel>> modelMigrationAsync = null)
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
            Func<TModel, DocumentVersion, ISerializerModifierAccessor, Task<TModel>> modelMigrationAsync = null)
            where TModel : class;
    }
}