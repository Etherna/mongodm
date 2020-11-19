using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    class ModelMapsSchema<TModel> : ModelMapsSchemaBase, IModelMapsSchemaBuilder<TModel>
        where TModel : class
    {
        // Constructor.
        public ModelMapsSchema(
            ModelMap<TModel> activeMap,
            IDbContext dbContext)
            : base(activeMap, dbContext, typeof(TModel))
        { }

        // Methods.
        public IModelMapsSchemaBuilder<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer)
        {
            AddFallbackCustomSerializerHelper(fallbackSerializer);
            return this;
        }

        public IModelMapsSchemaBuilder<TModel> AddSecondaryMap(
            string id,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            string? baseModelMapId = null,
            IBsonSerializer<TModel>? customSerializer = null)
        {
            // Verify if needs a default serializer.
            if (!typeof(TModel).IsAbstract)
                customSerializer ??= ModelMap.GetDefaultSerializer<TModel>(DbContext);

            // Create model map.
            var modelMap = new ModelMap<TModel>(
                id,
                new BsonClassMap<TModel>(modelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId,
                customSerializer);

            return AddSecondaryMap(modelMap);
        }

        public IModelMapsSchemaBuilder<TModel> AddSecondaryMap(ModelMap<TModel> modelMap)
        {
            AddSecondaryMapHelper(modelMap);
            return this;
        }
    }
}
