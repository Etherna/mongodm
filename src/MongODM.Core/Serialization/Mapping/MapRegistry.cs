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

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class MapRegistry : FreezableConfig, IMapRegistry
    {
        // Fields.
        private readonly Dictionary<Type, IMap> _maps = new(); //model type -> map
        private readonly Dictionary<string, IMemberMap> _memberMapsById = new();

        private readonly Dictionary<Type, BsonElement> activeModelMapIdBsonElement = new();
        private IDbContext dbContext = default!;
        private readonly ConcurrentDictionary<Type, BsonClassMap> defaultClassMapsCache = new();
        private ILogger logger = default!;
        private readonly Dictionary<IModelMap, Dictionary<string, List<IMemberMap>>> memberMapsByElementPath = new(); //model map -> element path -> member map[]
        private readonly Dictionary<MemberInfo, List<IMemberMap>> memberMapsByMemberInfo = new();

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
        public IReadOnlyDictionary<Type, IMap> MapsByModelType => _maps;
        public IReadOnlyDictionary<string, IMemberMap> MemberMapsById => _memberMapsById;

        // Methods.
        public ICustomSerializerMapBuilder<TModel> AddCustomSerializerMap<TModel>(
            IBsonSerializer<TModel> customSerializer) where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (customSerializer is null)
                    throw new ArgumentNullException(nameof(customSerializer));

                // Register and return map configuration.
                var customSerializerMap = new CustomSerializerMap<TModel>(customSerializer);
                _maps.Add(typeof(TModel), customSerializerMap);

                return customSerializerMap;
            });

        public IModelMapBuilder<TModel> AddModelMap<TModel>(
            string activeModelMapSchemaId,
            Action<BsonClassMap<TModel>>? activeModelMapSchemaInitializer = null,
            IBsonSerializer<TModel>? activeCustomSerializer = null)
            where TModel : class =>
            ExecuteConfigAction(() =>
            {
                // Register and add schema configuration.
                var modelMap = new ModelMap<TModel>(dbContext);
                _maps.Add(typeof(TModel), modelMap);

                // Create model map and set it as active in schema.
                var schema = new ModelMapSchema<TModel>(
                    activeModelMapSchemaId,
                    new BsonClassMap<TModel>(activeModelMapSchemaInitializer ?? (cm => cm.AutoMap())),
                    null,
                    null,
                    activeCustomSerializer,
                    modelMap);
                modelMap.ActiveSchema = schema;

                // If model schema uses proxy model, register a new one for proxy type.
                if (modelMap.ProxyModelType != null)
                {
                    var proxyModelMap = CreateNewDefaultModelMap(modelMap.ProxyModelType);
                    _maps.Add(modelMap.ProxyModelType, proxyModelMap);
                }

                return modelMap;
            });

        public BsonClassMap GetActiveClassMap(Type modelType)
        {
            // If a map is registered.
            if (_maps.TryGetValue(modelType, out IMap map) &&
                map is IModelMap modelMap)
                return modelMap.ActiveSchema.BsonClassMap;

            // If we don't have a model map, look for a default classmap, or create it.
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
            return memberMapsByMemberInfo.FirstOrDefault(p => p.Key.IsSameAs(memberInfo)).Value ??
                (IEnumerable<IMemberMap>)Array.Empty<IMemberMap>();
        }

        public IEnumerable<IMemberMap> GetMemberMapsWithSameElementPath(IMemberMap memberMap)
        {
            Freeze(); //needed for initialization
            return memberMapsByElementPath.TryGetValue(memberMap.MemberMapPath.First().ModelMapSchema.ModelMap, out var elementPathDictionary) &&
                elementPathDictionary.TryGetValue(GetMemberMapElementPath(memberMap), out var samePathMemberMaps) ?
                samePathMemberMaps :
                Array.Empty<IMemberMap>();
        }

        public IModelMap GetModelMap(Type modelType)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));
            if (!_maps.ContainsKey(modelType))
                throw new KeyNotFoundException(modelType.Name + " map is missing");

            var map = _maps[modelType];

            if (map is not IModelMap modelMap)
                throw new InvalidOperationException(modelType.Name + " map is not a model map");

            return modelMap;
        }

        public bool TryGetModelMap(Type modelType, out IModelMap? modelMap)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));

            if (_maps.TryGetValue(modelType, out IMap map) &&
                map is IModelMap foundModelMap)
            {
                modelMap = foundModelMap;
                return true;
            }

            modelMap = null;
            return false;
        }

        // Protected methods.
        protected override void FreezeAction()
        {
            // Link model maps with their base map.
            LinkBaseModelMaps();

            // Freeze, register serializers and compile registers.
            foreach (var map in _maps.Values)
            {
                // Freeze model map.
                map.Freeze();

                // Register active serializer.
                if (map.ActiveSerializer != null)
                    ((BsonSerializerRegistry)dbContext.SerializerRegistry).RegisterSerializer(map.ModelType, map.ActiveSerializer);

                // Register discriminators for all bson class maps.
                if (map is IModelMap modelMap)
                    foreach (var modelMapSchema in modelMap.SchemasById.Values)
                        dbContext.DiscriminatorRegistry.AddDiscriminator(modelMapSchema.BsonClassMap.ClassType, modelMapSchema.BsonClassMap.Discriminator);
            }

            // Specific for model maps.
            foreach (var modelMap in _maps.Values.OfType<ModelMap>())
            {
                // Initialize member maps.
                modelMap.InitializeMemberMaps();

                // Initialize member map registers.
                /*
                 * Only model map based schemas can be analyzed.
                 * Schemas based on custom serializers can't be explored.
                 * 
                 * Skip member map analysis of proxy models.
                 * 
                 * This operation needs to be executed AFTER that all serializers have been registered.
                 */
                if (!dbContext.ProxyGenerator.IsProxyType(modelMap.ModelType))
                {
                    foreach (var memberMap in modelMap.AllDescendingMemberMaps)
                    {
                        //map member map into registers
                        _memberMapsById[memberMap.Id] = memberMap;
                        MapMemberMapsByMemberInfo(memberMap);
                        MapMemberMapsByRootModelMapAndElementPath(memberMap);
                    }
                }

                // Generate active model maps id bson elements.
                /*
                 * If current model type is proxy, we need to use id of its base type. This because
                 * when we serialize a proxy model, we don't want that the proxy's model map id
                 * will be reported on document, but we want to serialize its original type's id.
                 */
                var notProxyModelMap = GetModelMap(dbContext.ProxyGenerator.PurgeProxyType(modelMap.ModelType));

                activeModelMapIdBsonElement.Add(
                    modelMap.ModelType,
                    new BsonElement(
                        dbContext.Options.ModelMapVersion.ElementName,
                        new BsonString(notProxyModelMap.ActiveSchema.Id)));
            }
        }

        // Helpers.
        private IModelMap CreateNewDefaultModelMap(Type modelType)
        {
            // Construct.
            //model schema
            var modelMapDefinition = typeof(ModelMap<>);
            var modelMapType = modelMapDefinition.MakeGenericType(modelType);

            var modelMap = (ModelMap)Activator.CreateInstance(
                modelMapType,
                dbContext);          //IDbContext dbContext

            //class map
            var classMapDefinition = typeof(BsonClassMap<>);
            var classMapType = classMapDefinition.MakeGenericType(modelType);

            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);

            //model map
            var modelMapSchemaDefinition = typeof(ModelMapSchema<>);
            var modelMapSchemaType = modelMapSchemaDefinition.MakeGenericType(modelType);

            var activeModelMapSchema = (ModelMapSchema)Activator.CreateInstance(
                modelMapSchemaType,
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new object[]
                {
                    Guid.NewGuid().ToString(), //string id
                    classMap,                  //BsonClassMap<TModel> bsonClassMap
                    null!,                     //string? baseModelMapId
                    null!,                     //Func<TModel, Task<TModel>>? fixDeserializedModelFunc
                    null!,                     //IBsonSerializer<TModel>? customSerializer
                    modelMap                   //IModelMap modelMap
                },
                CultureInfo.InvariantCulture);

            // Set active model map.
            modelMap.ActiveSchema = activeModelMapSchema;

            return modelMap;
        }

        private static string GetMemberMapElementPath(IMemberMap memberMap) => memberMap.RenderElementPath(false, _ => ".$", _ => ".*");

        private void LinkBaseModelMaps()
        {
            /* A stack with a while iteration is needed, instead of a foreach construct,
             * because we will add new schemas if needed. Foreach is based on enumerable
             * iterator, and if an enumerable is modified during foreach execution, an
             * exception is rised.
             */
            var processingModelMaps = new Stack<IModelMap>(_maps.Values.OfType<IModelMap>());

            while (processingModelMaps.Any())
            {
                var modelMap = processingModelMaps.Pop();

                // Process schema's model maps.
                foreach (var modelMapSchema in modelMap.SchemasById.Values)
                {
                    var baseModelType = modelMapSchema.BsonClassMap.ClassType.BaseType;

                    // If don't need to be linked, because it is typeof(object).
                    if (baseModelType is null)
                        continue;

                    // Get base type map, or generate it.
                    if (!_maps.TryGetValue(baseModelType, out IMap baseMap))
                    {
                        // Create schema instance.
                        baseMap = CreateNewDefaultModelMap(baseModelType);

                        // Register schema instance.
                        _maps.Add(baseModelType, baseMap);
                        processingModelMaps.Push((IModelMap)baseMap);
                    }

                    // Search base model map schema.
                    var baseModelMapSchema = modelMapSchema.BaseSchemaId != null ?
                        ((IModelMap)baseMap).SchemasById[modelMapSchema.BaseSchemaId] :
                        ((IModelMap)baseMap).ActiveSchema;

                    // Link base model map.
                    modelMapSchema.SetBaseModelMapSchema(baseModelMapSchema);
                }
            }
        }

        private void MapMemberMapsByMemberInfo(IMemberMap memberMap)
        {
            /*
             * MemberInfo comparison has to be performed with extension method "IsSameAs". If an equal member info
             * is found with this equality comparer, it has to be taken as key also for current memberinfo
             */
            var memberInfo = memberMap.BsonMemberMap.MemberInfo;
            var memberMapListByMemberInfo = memberMapsByMemberInfo.FirstOrDefault(pair => pair.Key.IsSameAs(memberInfo)).Value;

            if (memberMapListByMemberInfo is null)
            {
                memberMapListByMemberInfo = new List<IMemberMap>();
                memberMapsByMemberInfo[memberInfo] = memberMapListByMemberInfo;
            }

            memberMapListByMemberInfo.Add(memberMap);
        }

        private void MapMemberMapsByRootModelMapAndElementPath(IMemberMap memberMap)
        {
            var rootModelMap = memberMap.MemberMapPath.First().ModelMapSchema.ModelMap;
            var memberMapElementPath = GetMemberMapElementPath(memberMap);
            if (!memberMapsByElementPath.TryGetValue(rootModelMap, out var memberMapDictionaryByElementPath))
            {
                memberMapDictionaryByElementPath = new Dictionary<string, List<IMemberMap>>();
                memberMapsByElementPath[rootModelMap] = memberMapDictionaryByElementPath;
            }
            if (!memberMapDictionaryByElementPath.TryGetValue(memberMapElementPath, out var memberMapListByElementPath))
            {
                memberMapListByElementPath = new List<IMemberMap>();
                memberMapDictionaryByElementPath[memberMapElementPath] = memberMapListByElementPath;
            }

            memberMapListByElementPath.Add(memberMap);
        }
    }
}
