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
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Mapping.Schemas;
using Etherna.MongODM.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Serializers
{
    public class ReferenceSerializerConfiguration : FreezableConfig
    {
        // Fields.
        private readonly Dictionary<Type, IModelMapsSchema> _schemas = new();
        private bool _useCascadeDelete;

        private readonly Dictionary<Type, BsonElement> activeModelMapIdBsonElement = new();
        private readonly IDbContext dbContext;

        // Constructor.
        public ReferenceSerializerConfiguration(IDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // Properties.
        public IReadOnlyDictionary<Type, IModelMapsSchema> Schemas => _schemas;

        public bool UseCascadeDelete
        {
            get => _useCascadeDelete;
            set => ExecuteConfigAction(() => _useCascadeDelete = value);
        }

        // Methods.
        public IReferenceModelMapsSchemaBuilder<TModel> AddModelMapsSchema<TModel>(
            string activeModelMapId,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null,
            string? baseModelMapId = null)
            where TModel : class =>
            AddModelMapsSchema(new ModelMap<TModel>(
                activeModelMapId,
                new BsonClassMap<TModel>(activeModelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId));

        public IReferenceModelMapsSchemaBuilder<TModel> AddModelMapsSchema<TModel>(
            ModelMap<TModel> activeModelMap)
            where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (activeModelMap is null)
                    throw new ArgumentNullException(nameof(activeModelMap));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new ReferenceModelMapsSchema<TModel>(activeModelMap, dbContext);
                _schemas.Add(typeof(TModel), modelSchemaConfiguration);

                // If model maps schema uses proxy model, register a new one for proxy type.
                if (modelSchemaConfiguration.ProxyModelType != null)
                {
                    var proxyModelSchema = CreateNewDefaultReferenceSchema(modelSchemaConfiguration.ProxyModelType);
                    _schemas.Add(modelSchemaConfiguration.ProxyModelType, proxyModelSchema);
                }

                return modelSchemaConfiguration;
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

        public IBsonSerializer? GetSerializer(Type modelType, string? modelMapId)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));
            if (!_schemas.ContainsKey(modelType))
                throw new InvalidOperationException("Can't identify registered schema for type " + modelType.Name);

            // Find serializer.
            var schema = _schemas[modelType];

            //if a correct model map is identified with its id, use its bson class map serializer
            if (modelMapId != null && schema.AllModelMapsDictionary.ContainsKey(modelMapId))
                return schema.AllModelMapsDictionary[modelMapId].BsonClassMapSerializer;

            //else, use fallback serializer if exists. The schema's active serializer otherwise
            return schema.FallbackSerializer ?? schema.ActiveSerializer;
        }

        // Protected methods.
        protected override void FreezeAction()
        {
            // Link model maps with their base map.
            LinkBaseModelMaps();

            // Freeze and register bson elements.
            foreach (var schema in _schemas.Values)
            {
                // Freeze schemas.
                schema.Freeze();

                // Generate active model maps id bson elements.
                /*
                 * If current model type is proxy, we need to use id of its base type. This because
                 * when we serialize a proxy model, we don't want that in the proxy's model map id
                 * will be reported on document, but we want to serialize its original type's id.
                 */
                var notProxySchema = _schemas[dbContext.ProxyGenerator.PurgeProxyType(schema.ModelType)];

                activeModelMapIdBsonElement.Add(
                    schema.ModelType,
                    new BsonElement(
                        dbContext.Options.ModelMapVersion.ElementName,
                        new BsonString(notProxySchema.ActiveMap.Id)));
            }
        }

        // Helpers
        private IModelMapsSchema CreateNewDefaultReferenceSchema(Type modelType)
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
            var modelMapsSchemaDefinition = typeof(ReferenceModelMapsSchema<>);
            var modelMapsSchemaType = modelMapsSchemaDefinition.MakeGenericType(modelType);

            return (IModelMapsSchema)Activator.CreateInstance(
                modelMapsSchemaType,
                activeModelMap,      //ReferenceModelMap<TModel> activeMap
                dbContext);          //IDbContext dbContext
        }

        private void LinkBaseModelMaps()
        {
            /* A stack with a while iteration is needed, instead of a foreach construct,
             * because we will add new schemas if needed. Foreach is based on enumerable
             * iterator, and if an enumerable is modified during foreach execution, an
             * exception is rised.
             */
            var processingSchemas = new Stack<IModelMapsSchema>(_schemas.Values);

            while (processingSchemas.Any())
            {
                var schema = processingSchemas.Pop();
                var baseModelType = schema.ModelType.BaseType;

                // If don't need to be linked, because it is typeof(object).
                if (baseModelType is null)
                    continue;

                // Get base type schema, or generate it.
                if (!_schemas.TryGetValue(baseModelType, out IModelMapsSchema baseSchema))
                {
                    // Create schema instance.
                    baseSchema = CreateNewDefaultReferenceSchema(baseModelType);

                    // Register schema instance.
                    _schemas.Add(baseModelType, baseSchema);
                    processingSchemas.Push(baseSchema);
                }

                // Process schema's model maps.
                foreach (var modelMap in schema.AllModelMapsDictionary.Values)
                {
                    // Search base model map.
                    var baseModelMap = modelMap.BaseModelMapId != null ?
                        baseSchema.AllModelMapsDictionary[modelMap.BaseModelMapId] :
                        baseSchema.ActiveMap;

                    // Link base model map.
                    modelMap.SetBaseModelMap(baseModelMap);
                }
            }
        }
    }
}
