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
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Etherna.MongODM.Core.Serialization.Serializers
{
    public class ReferenceSerializerConfiguration : FreezableConfig
    {
        // Fields.
        private readonly Dictionary<Type, IModelMap> _modelMaps = new();

        private readonly Dictionary<Type, BsonElement> activeModelMapIdBsonElement = new();
        private readonly IDbContext dbContext;
        private readonly IReferenceSerializer serializer;

        // Constructor.
        internal ReferenceSerializerConfiguration(IDbContext dbContext, IReferenceSerializer serializer)
        {
            this.dbContext = dbContext;
            this.serializer = serializer;
        }

        // Properties.
        public IReadOnlyDictionary<Type, IModelMap> ModelMaps => _modelMaps;

        // Methods.
        public IModelMapBuilder<TModel> AddModelMap<TModel>(
            string activeModelMapSchemaId,
            Action<BsonClassMap<TModel>>? activeModelMapSchemaInitializer = null,
            string? baseSchemaId = null)
            where TModel : class =>
            ExecuteConfigAction(() =>
            {
                // Register and return schema configuration.
                var modelMap = new ModelMap<TModel>(dbContext);
                _modelMaps.Add(typeof(TModel), modelMap);

                // Create model map and set it as active in schema.
                var modelMapSchema = new ModelMapSchema<TModel>(
                    activeModelMapSchemaId,
                    new BsonClassMap<TModel>(activeModelMapSchemaInitializer ?? (cm => cm.AutoMap())),
                    baseSchemaId,
                    null,
                    serializer,
                    modelMap);
                modelMap.ActiveSchema = modelMapSchema;

                // If model schema uses proxy model, register a new one for proxy type.
                if (modelMap.ProxyModelType != null)
                {
                    var proxyModelMap = CreateNewDefaultModelMap(modelMap.ProxyModelType);
                    _modelMaps.Add(modelMap.ProxyModelType, proxyModelMap);
                }

                return modelMap;
            });

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

        public IBsonSerializer? GetSerializer(Type modelType, string? modelMapSchemaId)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));
            if (!_modelMaps.ContainsKey(modelType))
                throw new InvalidOperationException("Can't identify registered schema for type " + modelType.Name);

            // Find serializer.
            var modelMap = _modelMaps[modelType];

            //if a correct model map is identified with its id, use its bson class map serializer
            if (modelMapSchemaId != null && modelMap.SchemasById.ContainsKey(modelMapSchemaId))
                return modelMap.SchemasById[modelMapSchemaId].BsonClassMap.ToSerializer();

            //else, use fallback serializer if exists, or the active schema's bsonClassMap serializer
            return modelMap.FallbackSerializer ?? modelMap.ActiveSchema.BsonClassMap.ToSerializer();
        }

        // Protected methods.
        protected override void FreezeAction()
        {
            // Link model maps with their base map.
            LinkBaseModelMaps();

            // Freeze and register bson elements.
            foreach (var modelMap in _modelMaps.Values)
            {
                // Freeze model map.
                modelMap.Freeze();

                // Generate active model maps id bson elements.
                /*
                 * If current model type is proxy, we need to use id of its base type. This because
                 * when we serialize a proxy model, we don't want that in the proxy's model map id
                 * will be reported on document, but we want to serialize its original type's id.
                 */
                var notProxySchema = _modelMaps[dbContext.ProxyGenerator.PurgeProxyType(modelMap.ModelType)];

                activeModelMapIdBsonElement.Add(
                    modelMap.ModelType,
                    new BsonElement(
                        dbContext.Options.ModelMapVersion.ElementName,
                        new BsonString(notProxySchema.ActiveSchema.Id)));
            }
        }

        // Helpers
        private IModelMap CreateNewDefaultModelMap(Type modelType)
        {
            //model schema
            var modelSchemaDefinition = typeof(ModelMap<>);
            var modelSchemaType = modelSchemaDefinition.MakeGenericType(modelType);

            var modelSchema = (ModelMap)Activator.CreateInstance(
                modelSchemaType,
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
                    modelSchema                //IModelSchema schema
                },
                CultureInfo.InvariantCulture);

            // Set active model map.
            modelSchema.ActiveSchema = activeModelMapSchema;

            return modelSchema;
        }

        private void LinkBaseModelMaps()
        {
            /* A stack with a while iteration is needed, instead of a foreach construct,
             * because we will add new schemas if needed. Foreach is based on enumerable
             * iterator, and if an enumerable is modified during foreach execution, an
             * exception is rised.
             */
            var processingModelMaps = new Stack<IModelMap>(_modelMaps.Values);

            while (processingModelMaps.Any())
            {
                var modelMap = processingModelMaps.Pop();
                var baseModelType = modelMap.ModelType.BaseType;

                // If don't need to be linked, because it is typeof(object).
                if (baseModelType is null)
                    continue;

                // Get base type schema, or generate it.
                if (!_modelMaps.TryGetValue(baseModelType, out IModelMap baseModelMap))
                {
                    // Create schema instance.
                    baseModelMap = CreateNewDefaultModelMap(baseModelType);

                    // Register schema instance.
                    _modelMaps.Add(baseModelType, baseModelMap);
                    processingModelMaps.Push(baseModelMap);
                }

                // Process model maps' schemas.
                foreach (var modelMapSchema in modelMap.SchemasById.Values)
                {
                    // Search base model map.
                    var baseModelMapSchema = modelMapSchema.BaseSchemaId != null ?
                        baseModelMap.SchemasById[modelMapSchema.BaseSchemaId] :
                        baseModelMap.ActiveSchema;

                    // Link base model map.
                    modelMapSchema.SetBaseModelMapSchema(baseModelMapSchema);
                }
            }
        }
    }
}
