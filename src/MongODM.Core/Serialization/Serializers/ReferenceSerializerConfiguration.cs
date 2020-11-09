using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Mapping.Schemas;
using Etherna.MongODM.Core.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Serializers
{
    public class ReferenceSerializerConfiguration : FreezableConfig
    {
        // Fields.
        private readonly Dictionary<Type, IModelMapsSchema> _schemas = new Dictionary<Type, IModelMapsSchema>();
        private bool _useCascadeDelete;
        private readonly IDictionary<Type, BsonElement> activeModelMapIdBsonElement = new Dictionary<Type, BsonElement>();
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
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Don't need to dispose")]
        public IReferenceModelMapsSchema<TModel> AddModelMapsSchema<TModel>(
            string activeModelMapId,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null,
            string? baseModelMapId = null)
            where TModel : class =>
            AddModelMapsSchema(new ReferenceModelMap<TModel>(
                activeModelMapId,
                new BsonClassMap<TModel>(activeModelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId));

        public IReferenceModelMapsSchema<TModel> AddModelMapsSchema<TModel>(
            ReferenceModelMap<TModel> activeModelMap)
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

            return activeModelMapIdBsonElement[modelType];
        }

        public IBsonSerializer? GetActiveSerializer(Type modelType)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));
            if (!_schemas.ContainsKey(modelType))
                throw new InvalidOperationException("Can't identify registered schema for type " + modelType.Name);

            return Schemas[modelType].ActiveSerializer;
        }

        public IReferenceModelMapsSchema<TModel> GetModelMapsSchema<TModel>()
            where TModel : class =>
            (IReferenceModelMapsSchema<TModel>)Schemas[typeof(TModel)];

        public IBsonSerializer? GetSerializer(Type modelType, string? modelMapId)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));
            if (!_schemas.ContainsKey(modelType))
                throw new InvalidOperationException("Can't identify registered schema for type " + modelType.Name);

            // Find serializer.
            var schema = _schemas[modelType];

            //if a correct model map is identified with its id
            if (modelMapId != null && schema.AllMapsDictionary.ContainsKey(modelMapId))
                return schema.AllMapsDictionary[modelMapId].Serializer;

            //else, use fallback serializer if exists. The schema's active serializer otherwise
            return schema.FallbackSerializer ?? schema.ActiveSerializer;
        }

        // Protected methods.
        protected override void FreezeAction()
        {
            // Link model maps with their base map.
            LinkBaseModelMaps();

            // Freeze and register serializers.
            foreach (var schema in _schemas.Values)
            {
                // Freeze schemas.
                schema.Freeze();

                // Generate active model maps id bson elements.
                activeModelMapIdBsonElement.Add(
                    schema.ModelType,
                    new BsonElement(
                        dbContext.ModelMapVersionOptions.ElementName,
                        new BsonString(schema.ActiveMap.Id)));
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
            var modelMapDefinition = typeof(ReferenceModelMap<>);
            var modelMapType = modelMapDefinition.MakeGenericType(modelType);

            var activeModelMap = (ModelMapBase)Activator.CreateInstance(
                modelMapType,
                Guid.NewGuid().ToString(), //string id
                classMap,                  //BsonClassMap<TModel> bsonClassMap
                null);                     //string? baseModelMapId

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
                foreach (var modelMap in schema.AllMapsDictionary.Values)
                {
                    // Search base model map.
                    var baseModelMap = modelMap.BaseModelMapId != null ?
                        baseSchema.AllMapsDictionary[modelMap.BaseModelMapId] :
                        baseSchema.ActiveMap;

                    // Link base model map.
                    modelMap.SetBaseModelMap(baseModelMap);
                }
            }
        }
    }
}
