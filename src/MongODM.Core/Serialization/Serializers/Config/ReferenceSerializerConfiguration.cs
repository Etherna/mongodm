using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Etherna.MongODM.Core.Serialization.Serializers.Config
{
    public class ReferenceSerializerConfiguration : FreezableConfig
    {
        // Fields.
        private readonly Dictionary<Type, IReferenceSchema> _schemas = new Dictionary<Type, IReferenceSchema>();
        private bool _useCascadeDelete;
        private readonly IDbContext dbContext;
        private readonly IDictionary<(Type, string), IBsonSerializer> registeredSerializers = new Dictionary<(Type, string), IBsonSerializer>();

        // Constructor.
        public ReferenceSerializerConfiguration(IDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // Properties.
        public IReadOnlyDictionary<Type, IReferenceSchema> Schemas
        {
            get
            {
                Freeze();
                return _schemas;
            }
        }

        public bool UseCascadeDelete
        {
            get => _useCascadeDelete;
            set => ExecuteConfigAction(() => _useCascadeDelete = value);
        }

        // Methods.
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Don't need to dispose")]
        public IReferenceSchema<TModel> AddModelMapSchema<TModel>(
            string activeModelMapId,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null)
            where TModel : class =>
            AddModelMapSchema(new ReferenceModelMap<TModel>(
                activeModelMapId,
                new BsonClassMap<TModel>(activeModelMapInitializer ?? (cm => cm.AutoMap()))));

        public IReferenceSchema<TModel> AddModelMapSchema<TModel>(
            ReferenceModelMap<TModel> activeModelMap)
            where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (activeModelMap is null)
                    throw new ArgumentNullException(nameof(activeModelMap));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new ReferenceSchema<TModel>(activeModelMap, dbContext);
                _schemas.Add(typeof(TModel), modelSchemaConfiguration);

                return modelSchemaConfiguration;
            });

        public IBsonSerializer GetActiveModelMapSerializer(Type modelType)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));
            if (!_schemas.ContainsKey(modelType))
                throw new InvalidOperationException("Can't identify registered schema for type " + modelType.Name);

            Freeze();

            return registeredSerializers[(modelType, _schemas[modelType].ActiveMap.Id)];
        }

        public IReferenceSchema<TModel> GetModelMapsSchema<TModel>()
            where TModel : class =>
            (IReferenceSchema<TModel>)Schemas[typeof(TModel)];

        public IBsonSerializer GetSerializer(Type modelType, string? modelMapId)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));

            Freeze();

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
                else return GetActiveModelMapSerializer(modelType);
            }

            //else, throw an exception of unregistered schema
            else throw new InvalidOperationException(
                "Can't identify registered schema for type " + modelType.Name);
        }

        // Protected methods.
        protected override void FreezeAction()
        {
            foreach (var schema in _schemas.Values)
            {
                // Freeze schemas.
                schema.Freeze();

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
    }
}
