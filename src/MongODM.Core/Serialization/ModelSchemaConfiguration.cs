using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Serialization
{
    class ModelSchemaConfiguration<TModel> : IModelSchemaConfiguration<TModel>
        where TModel : class
    {
        // Fields.
        private readonly List<ModelSchema> _secondaryModelSchemas = new List<ModelSchema>();

        // Constructor.
        public ModelSchemaConfiguration(
            ModelSchema<TModel> activeModelSchema,
            bool requireCollectionMigration = false)
        {
            ActiveModelSchema = activeModelSchema;
            RequireCollectionMigration = requireCollectionMigration;
        }

        // Properties.
        public ModelSchema ActiveModelSchema { get; }
        public Type ModelType => typeof(TModel);
        public bool RequireCollectionMigration { get; }
        public IEnumerable<ModelSchema> SecondaryModelSchemas => _secondaryModelSchemas;

        // Methods.
        public IModelSchemaConfiguration<TModel> AddSecondarySchema(
            string id,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null) =>
            AddSecondarySchema(ModelSchemaBuilder.GenerateModelSchema(id, modelMapInitializer, customSerializer));

        public IModelSchemaConfiguration<TModel> AddSecondarySchema(ModelSchema<TModel> modelSchema)
        {
            _secondaryModelSchemas.Add(modelSchema);
            return this;
        }
    }
}
