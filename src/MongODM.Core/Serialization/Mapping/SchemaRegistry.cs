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
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Serialization.Mapping.Schemas;
using Etherna.MongODM.Core.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class SchemaRegistry : FreezableConfig, ISchemaRegistry
    {
        // Fields.
        private readonly Dictionary<string, IMemberMap> _memberMapsDictionary = new();
        private readonly Dictionary<Type, ISchema> _schemas = new(); //model type -> model schema

        private readonly Dictionary<Type, BsonElement> activeModelMapIdBsonElement = new();
        private readonly ConcurrentDictionary<Type, BsonClassMap> defaultClassMapsCache = new();
        private ILogger logger = default!;
        private readonly Dictionary<MemberInfo, List<IMemberMap>> memberInfoToMemberMapsDictionary = new();

        private IDbContext dbContext = default!;

        // Constructor and initializer.
        public void Initialize(IDbContext dbContext, ILogger logger)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            IsInitialized = true;

            this.logger.SchemaRegistryInitialized(dbContext.Options.DbName);
        }

        // Properties.
        public bool IsInitialized { get; private set; }
        public Dictionary<string, IMemberMap> MemberMapsDictionary => _memberMapsDictionary;
        public IReadOnlyDictionary<Type, ISchema> Schemas => _schemas;

        // Methods.
        public ICustomSerializerSchemaBuilder<TModel> AddCustomSerializerSchema<TModel>(
            IBsonSerializer<TModel> customSerializer) where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (customSerializer is null)
                    throw new ArgumentNullException(nameof(customSerializer));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new CustomSerializerSchema<TModel>(customSerializer);
                _schemas.Add(typeof(TModel), modelSchemaConfiguration);

                return modelSchemaConfiguration;
            });

        public IModelMapsSchemaBuilder<TModel> AddModelMapsSchema<TModel>(
            string activeModelMapId,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null) where TModel : class
        {
            // Create model map.
            var modelMap = new ModelMap<TModel>(
                activeModelMapId,
                new BsonClassMap<TModel>(activeModelMapInitializer ?? (cm => cm.AutoMap())),
                serializer: customSerializer ?? ModelMap.GetDefaultSerializer<TModel>(dbContext));

            return AddModelMapsSchema(modelMap);
        }

        public IModelMapsSchemaBuilder<TModel> AddModelMapsSchema<TModel>(
            ModelMap<TModel> activeModelMap)
            where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (activeModelMap is null)
                    throw new ArgumentNullException(nameof(activeModelMap));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new ModelMapsSchema<TModel>(activeModelMap, dbContext);
                _schemas.Add(typeof(TModel), modelSchemaConfiguration);

                // If model maps schema uses proxy model, register a new one for proxy type.
                if (modelSchemaConfiguration.ProxyModelType != null)
                {
                    var proxyModelSchema = CreateNewDefaultModelMapsSchema(modelSchemaConfiguration.ProxyModelType);
                    _schemas.Add(modelSchemaConfiguration.ProxyModelType, proxyModelSchema);
                }

                return modelSchemaConfiguration;
            });

        public BsonClassMap GetActiveClassMap(Type modelType)
        {
            // If a schema is registered.
            if (_schemas.TryGetValue(modelType, out ISchema schema) &&
                schema is IModelMapsSchema modelMapSchema)
                return modelMapSchema.ActiveModelMap.BsonClassMap;

            // If we don't have a model schema, look for a default classmap, or create it.
            if (defaultClassMapsCache.TryGetValue(modelType, out BsonClassMap bcm))
                return bcm;

            var classMapDefinition = typeof(BsonClassMap<>);
            var classMapType = classMapDefinition.MakeGenericType(modelType);
            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);
            classMap.AutoMap();

            // Register classMap (if doesn't exist) with discriminator.
            defaultClassMapsCache.TryAdd(modelType, classMap);
            dbContext.DiscriminatorRegistry.AddDiscriminator(modelType, classMap.Discriminator);

            return classMap;
        }

        public BsonElement GetActiveModelMapIdBsonElement(Type modelType)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));

            Freeze(); //needed for initialization

            /*
             * Use of this cache dictionary avoids checks and creation of new bson elements
             * for each serialization.
             */
            return activeModelMapIdBsonElement[modelType];
        }

        public IEnumerable<IMemberMap> GetMemberMapsFromMemberInfo(MemberInfo memberInfo)
        {
            Freeze(); //needed for initialization

            foreach (var pair in memberInfoToMemberMapsDictionary)
                if (pair.Key.IsSameAs(memberInfo))
                    return pair.Value;

            return Array.Empty<IMemberMap>();
        }

        public IModelMapsSchema GetModelMapsSchema(Type modelType)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));
            if (!_schemas.ContainsKey(modelType))
                throw new KeyNotFoundException(modelType.Name + " schema is not registered");

            var schema = _schemas[modelType];

            if (schema is not IModelMapsSchema modelMapSchema)
                throw new InvalidOperationException(modelType.Name + " schema is not a model maps schema");

            return modelMapSchema;
        }

        public bool TryGetModelMapsSchema(Type modelType, out IModelMapsSchema? modelMapsSchema)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));

            if (_schemas.TryGetValue(modelType, out ISchema schema) &&
                schema is IModelMapsSchema foundModelMapsSchema)
            {
                modelMapsSchema = foundModelMapsSchema;
                return true;
            }

            modelMapsSchema = null;
            return false;
        }

        // Protected methods.
        protected override void FreezeAction()
        {
            // Link model maps with their base map.
            LinkBaseModelMaps();

            // Freeze, register serializers and compile registers.
            foreach (var schema in _schemas.Values)
            {
                // Freeze schema.
                schema.Freeze();

                // Register active serializer.
                if (schema.ActiveSerializer != null)
                    ((BsonSerializerRegistry)dbContext.SerializerRegistry).RegisterSerializer(schema.ModelType, schema.ActiveSerializer);

                // Register discriminators for all bson class maps.
                if (schema is IModelMapsSchema modelMapsSchema)
                    foreach (var modelMap in modelMapsSchema.RootModelMapsDictionary.Values)
                        dbContext.DiscriminatorRegistry.AddDiscriminator(modelMap.ModelType, modelMap.BsonClassMap.Discriminator);
            }

            // Specific for model maps schemas.
            foreach (var schema in _schemas.Values.OfType<IModelMapsSchema>())
            {
                // Compile model maps registers.
                /*
                 * Only model map based schemas can be analyzed.
                 * Schemas based on custom serializers can't be explored.
                 * 
                 * This operation needs to be executed AFTER that all serializers have been registered.
                 */
                foreach (var memberMap in schema.RootModelMapsDictionary.Values.SelectMany(modelMap => modelMap.AllChildMemberMapsDictionary.Values))
                {
                    //map member map with its Id
                    _memberMapsDictionary.Add(memberMap.Id, memberMap);

                    //map memberInfo to related member dependencies
                    /*
                     * MemberInfo comparison has to be performed with extension method "IsSameAs". If an equal member info
                     * is found with this equality comparer, it has to be taken as key also for current memberinfo
                     */
                    var memberInfo = memberMap.BsonMemberMap.MemberInfo;
                    var memberMapList = memberInfoToMemberMapsDictionary.FirstOrDefault(
                        pair => pair.Key.IsSameAs(memberInfo)).Value;

                    if (memberMapList is null)
                    {
                        memberMapList = new List<IMemberMap>();
                        memberInfoToMemberMapsDictionary[memberInfo] = memberMapList;
                    }

                    memberMapList.Add(memberMap);
                }

                // Generate active model maps id bson elements.
                /*
                 * If current model type is proxy, we need to use id of its base type. This because
                 * when we serialize a proxy model, we don't want that in the proxy's model map id
                 * will be reported on document, but we want to serialize its original type's id.
                 */
                var notProxySchema = GetModelMapsSchema(dbContext.ProxyGenerator.PurgeProxyType(schema.ModelType));

                activeModelMapIdBsonElement.Add(
                    schema.ModelType,
                    new BsonElement(
                        dbContext.Options.ModelMapVersion.ElementName,
                        new BsonString(notProxySchema.ActiveModelMap.Id)));
            }
        }

        // Helpers.
        private IModelMapsSchema CreateNewDefaultModelMapsSchema(Type modelType)
        {
            //class map
            var classMapDefinition = typeof(BsonClassMap<>);
            var classMapType = classMapDefinition.MakeGenericType(modelType);

            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);

            //model map
            var modelMapDefinition = typeof(ModelMap<>);
            var modelMapType = modelMapDefinition.MakeGenericType(modelType);

            var activeModelMap = (ModelMap)Activator.CreateInstance(
                modelMapType,
                Guid.NewGuid().ToString(), //string id
                classMap,                  //BsonClassMap<TModel> bsonClassMap
                null,                      //string? baseModelMapId
                null,                      //Func<TModel, Task<TModel>>? fixDeserializedModelFunc
                null);                     //IBsonSerializer<TModel>? serializer

            //model maps schema
            var modelMapsSchemaDefinition = typeof(ModelMapsSchema<>);
            var modelMapsSchemaType = modelMapsSchemaDefinition.MakeGenericType(modelType);

            return (IModelMapsSchema)Activator.CreateInstance(
                modelMapsSchemaType,
                activeModelMap,      //ModelMap<TModel> activeMap
                dbContext);          //IDbContext dbContext
        }

        private void LinkBaseModelMaps()
        {
            /* A stack with a while iteration is needed, instead of a foreach construct,
             * because we will add new schemas if needed. Foreach is based on enumerable
             * iterator, and if an enumerable is modified during foreach execution, an
             * exception is rised.
             */
            var processingSchemas = new Stack<IModelMapsSchema>(_schemas.Values.OfType<IModelMapsSchema>());

            while (processingSchemas.Any())
            {
                var schema = processingSchemas.Pop();

                // Process schema's model maps.
                foreach (var modelMap in schema.RootModelMapsDictionary.Values)
                {
                    var baseModelType = modelMap.ModelType.BaseType;

                    // If don't need to be linked, because it is typeof(object).
                    if (baseModelType is null)
                        continue;

                    // Get base type schema, or generate it.
                    if (!_schemas.TryGetValue(baseModelType, out ISchema baseSchema))
                    {
                        // Create schema instance.
                        baseSchema = CreateNewDefaultModelMapsSchema(baseModelType);

                        // Register schema instance.
                        _schemas.Add(baseModelType, baseSchema);
                        processingSchemas.Push((IModelMapsSchema)baseSchema);
                    }

                    // Search base model map.
                    var baseModelMap = modelMap.BaseModelMapId != null ?
                        ((IModelMapsSchema)baseSchema).RootModelMapsDictionary[modelMap.BaseModelMapId] :
                        ((IModelMapsSchema)baseSchema).ActiveModelMap;

                    // Link base model map.
                    modelMap.SetBaseModelMap(baseModelMap);
                }
            }
        }
    }
}
