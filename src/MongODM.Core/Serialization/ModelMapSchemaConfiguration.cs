using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Serialization
{
    class ModelMapSchemaConfiguration<TModel> : SchemaConfigurationBase, IModelMapSchemaConfiguration<TModel>
        where TModel : class
    {
        // Fields.
        private readonly List<ModelMapSchema> _secondarySchemas = new List<ModelMapSchema>();

        // Constructor.
        public ModelMapSchemaConfiguration(
            ModelMapSchema<TModel> activeModelSchema,
            bool requireCollectionMigration = false)
            : base(typeof(TModel), requireCollectionMigration)
        {
            ActiveSchema = activeModelSchema;
        }

        // Properties.
        public ModelMapSchema ActiveSchema { get; }
        public IBsonSerializer? FallbackSerializer { get; private set; }
        public IEnumerable<ModelMapSchema> SecondarySchemas => _secondarySchemas;

        // Methods.
        public IModelMapSchemaConfiguration<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer)
        {
            if (fallbackSerializer is null)
                throw new ArgumentNullException(nameof(fallbackSerializer));
            if (FallbackSerializer != null)
                throw new InvalidOperationException("Fallback serializer already setted");

            FallbackSerializer = fallbackSerializer;
            return this;
        }

        public IModelMapSchemaConfiguration<TModel> AddSecondarySchema(
            string id,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null) =>
            AddSecondarySchema(new ModelMapSchema<TModel>(
                id,
                new BsonClassMap<TModel>(modelMapInitializer ?? (cm => cm.AutoMap())),
                customSerializer));

        public IModelMapSchemaConfiguration<TModel> AddSecondarySchema(ModelMapSchema<TModel> modelSchema)
        {
            if (modelSchema is null)
                throw new ArgumentNullException(nameof(modelSchema));

            _secondarySchemas.Add(modelSchema);
            return this;
        }
    }
}
