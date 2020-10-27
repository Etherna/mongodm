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
        private readonly IDbContext dbContext;

        // Constructor.
        public ModelMapSchemaConfiguration(
            ModelMapSchema<TModel> activeModelSchema,
            IDbContext dbContext,
            bool requireCollectionMigration = false)
            : base(typeof(TModel), requireCollectionMigration)
        {
            ActiveSchema = activeModelSchema ?? throw new ArgumentNullException(nameof(activeModelSchema));
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Verify if have to use proxy model.
            if (UseProxyModel)
            {
                ProxyModelType = dbContext.ProxyGenerator.CreateInstance(ModelType, dbContext).GetType();
                activeModelSchema.UseProxyGenerator(dbContext);
            }

            // Verify if needs to use default serializer.
            if (!typeof(TModel).IsAbstract && activeModelSchema.Serializer is null)
                activeModelSchema.UseDefaultSerializer(dbContext);
        }

        // Properties.
        public ModelMapSchema ActiveSchema { get; }
        public override IBsonSerializer? ActiveSerializer => ActiveSchema.Serializer;
        public IBsonSerializer? FallbackSerializer { get; private set; }
        public override Type? ProxyModelType { get; }
        public IEnumerable<ModelMapSchema> SecondarySchemas => _secondarySchemas;
        public override bool UseProxyModel => !typeof(TModel).IsAbstract;

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

            // Verify if have to use proxy model.
            if (UseProxyModel)
                modelSchema.UseProxyGenerator(dbContext);

            // Add schema.
            _secondarySchemas.Add(modelSchema);
            return this;
        }
    }
}
