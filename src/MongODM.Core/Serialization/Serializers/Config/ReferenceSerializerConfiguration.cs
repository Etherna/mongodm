using Etherna.MongODM.Core.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Serializers.Config
{
    public class ReferenceSerializerConfiguration : FreezableConfig
    {
        // Fields.
        private readonly Dictionary<Type, IReferenceSchema> _schemas = new Dictionary<Type, IReferenceSchema>();
        private bool _useCascadeDelete;
        private readonly IDictionary<Type, BsonElement> activeModelMapIdBsonElement = new Dictionary<Type, BsonElement>();
        private readonly IDbContext dbContext;
        private readonly IDictionary<(Type, string), IBsonSerializer> registeredSerializers = new Dictionary<(Type, string), IBsonSerializer>();

        // Constructor.
        public ReferenceSerializerConfiguration(IDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // Properties.
        public IReadOnlyDictionary<Type, IReferenceSchema> Schemas => _schemas;

        public bool UseCascadeDelete
        {
            get => _useCascadeDelete;
            set => ExecuteConfigAction(() => _useCascadeDelete = value);
        }

        // Methods.
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Don't need to dispose")]
        public IReferenceSchema<TModel> AddModelMapsSchema<TModel>(
            string activeModelMapId,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null,
            string? baseModelMapId = null)
            where TModel : class =>
            AddModelMapsSchema(new ReferenceModelMap<TModel>(
                activeModelMapId,
                new BsonClassMap<TModel>(activeModelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId));

        public IReferenceSchema<TModel> AddModelMapsSchema<TModel>(
            ReferenceModelMap<TModel> activeModelMap)
            where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (activeModelMap is null)
                    throw new ArgumentNullException(nameof(activeModelMap));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new ReferenceSchema<TModel>(activeModelMap, dbContext);
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

        public IBsonSerializer GetActiveSerializer(Type modelType)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));
            if (!_schemas.ContainsKey(modelType))
                throw new InvalidOperationException("Can't identify registered schema for type " + modelType.Name);

            Freeze(); //needed for initialization

            return registeredSerializers[(modelType, _schemas[modelType].ActiveMap.Id)];
        }

        public IReferenceSchema<TModel> GetModelMapsSchema<TModel>()
            where TModel : class =>
            (IReferenceSchema<TModel>)Schemas[typeof(TModel)];

        public IBsonSerializer GetSerializer(Type modelType, string? modelMapId)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));

            Freeze(); //needed for initialization

            // Find serializer.
            //if a correct model map is identified with its id
            if (modelMapId != null &&
                registeredSerializers.ContainsKey((modelType, modelMapId)))
                return registeredSerializers[(modelType, modelMapId)];

            //else, verify if schema has been correctly registered
            else if (_schemas.ContainsKey(modelType))
            {
                var schema = _schemas[modelType];

                //if a fallback serializator exists, use it
                if (schema.FallbackSerializer != null)
                    return schema.FallbackSerializer;

                //else, deserialize wih current active model map
                else return GetActiveSerializer(modelType);
            }

            //else, throw an exception of unregistered schema
            else throw new InvalidOperationException(
                "Can't identify registered schema for type " + modelType.Name);
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

                // Generate serializers.
                foreach (var modelMap in schema.AllMapsDictionary.Values)
                {
                    var classMapSerializerDefinition = typeof(BsonClassMapSerializer<>);
                    var classMapSerializerType = classMapSerializerDefinition.MakeGenericType(modelMap.ModelType);
                    var serializer = (IBsonSerializer)Activator.CreateInstance(classMapSerializerType, modelMap.BsonClassMap);
                    registeredSerializers.Add((modelMap.ModelType, modelMap.Id), serializer);
                }
            }
        }

        // Helpers
        private IReferenceSchema CreateNewDefaultReferenceSchema(Type modelType)
        {
            //class map
            var classMapDefinition = typeof(BsonClassMap<>);
            var classMapType = classMapDefinition.MakeGenericType(modelType);

            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);

            //model map
            var modelMapDefinition = typeof(ReferenceModelMap<>);
            var modelMapType = modelMapDefinition.MakeGenericType(modelType);

            var activeModelMap = (ReferenceModelMap)Activator.CreateInstance(
                modelMapType,
                Guid.NewGuid().ToString(), //string id
                classMap,                  //BsonClassMap<TModel> bsonClassMap
                null);                     //string? baseModelMapId

            //model maps schema
            var modelMapsSchemaDefinition = typeof(ReferenceSchema<>);
            var modelMapsSchemaType = modelMapsSchemaDefinition.MakeGenericType(modelType);

            return (IReferenceSchema)Activator.CreateInstance(
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
            var processingSchemas = new Stack<IReferenceSchema>(_schemas.Values);

            while (processingSchemas.Any())
            {
                var schema = processingSchemas.Pop();
                var baseModelType = schema.ModelType.BaseType;

                // If don't need to be linked, because it is typeof(object).
                if (baseModelType is null)
                    continue;

                // Get base type schema, or generate it.
                if (!_schemas.TryGetValue(baseModelType, out IReferenceSchema baseSchema))
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
