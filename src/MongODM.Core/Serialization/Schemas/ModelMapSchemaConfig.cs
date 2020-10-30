using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Schemas
{
    class ModelMapSchemaConfig<TModel> : SchemaConfigBase, IModelMapSchemaConfig<TModel>
        where TModel : class
    {
        // Fields.
        private readonly ModelMapSchema _activeSchema;
        private readonly List<ModelMapSchema> _secondarySchemas = new List<ModelMapSchema>();
        private IBsonSerializer? _fallbackSerializer;
        private IDictionary<string, ModelMapSchema> _schemaDictionary = default!;
        private readonly IDbContext dbContext;

        // Constructor.
        public ModelMapSchemaConfig(
            ModelMapSchema<TModel> activeSchema,
            IDbContext dbContext,
            bool requireCollectionMigration = false)
            : base(typeof(TModel), requireCollectionMigration)
        {
            _activeSchema = activeSchema ?? throw new ArgumentNullException(nameof(activeSchema));
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Verify if have to use proxy model.
            if (UseProxyModel)
            {
                ProxyModelType = dbContext.ProxyGenerator.CreateInstance(ModelType, dbContext).GetType();
                activeSchema.UseProxyGenerator(dbContext);
            }

            // Verify if needs to use default serializer.
            if (!typeof(TModel).IsAbstract && activeSchema.Serializer is null)
                activeSchema.UseDefaultSerializer(dbContext);
        }

        // Properties.
        public ModelMapSchema ActiveSchema
        {
            get
            {
                Freeze();
                return _activeSchema;
            }
        }
        public override IBsonSerializer? ActiveSerializer => ActiveSchema.Serializer;
        public IBsonSerializer? FallbackSerializer
        {
            get
            {
                Freeze();
                return _fallbackSerializer;
            }
        }
        public override Type? ProxyModelType { get; }
        public IDictionary<string, ModelMapSchema> SchemaDictionary
        {
            get
            {
                Freeze();

                if (_schemaDictionary is null)
                {
                    // Build schema dictionary.
                    _schemaDictionary = SecondarySchemas
                        .Append(ActiveSchema)
                        .ToDictionary(schema => schema.Id);
                }
                return _schemaDictionary;
            }
        }
        public IEnumerable<ModelMapSchema> SecondarySchemas
        {
            get
            {
                Freeze();
                return _secondarySchemas;
            }
        }
        public override bool UseProxyModel => !typeof(TModel).IsAbstract;

        // Methods.
        public IModelMapSchemaConfig<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer) =>
            ExecuteConfigAction(() =>
            {
                if (fallbackSerializer is null)
                    throw new ArgumentNullException(nameof(fallbackSerializer));
                if (_fallbackSerializer != null)
                    throw new InvalidOperationException("Fallback serializer already setted");

                _fallbackSerializer = fallbackSerializer;

                return this;
            });

        public IModelMapSchemaConfig<TModel> AddSecondarySchema(
            string id,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null) =>
            AddSecondarySchema(new ModelMapSchema<TModel>(
                id,
                new BsonClassMap<TModel>(modelMapInitializer ?? (cm => cm.AutoMap())),
                customSerializer));

        public IModelMapSchemaConfig<TModel> AddSecondarySchema(ModelMapSchema<TModel> modelSchema) =>
            ExecuteConfigAction(() =>
            {
                if (modelSchema is null)
                    throw new ArgumentNullException(nameof(modelSchema));

                // Verify if have to use proxy model.
                if (UseProxyModel)
                    modelSchema.UseProxyGenerator(dbContext);

                // Add schema.
                _secondarySchemas.Add(modelSchema);
                return this;
            });
    }
}
