using MongoDB.Bson.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    class ModelMapsSchema<TModel> : ModelMapsSchemaBase, IModelMapsSchema<TModel>
        where TModel : class
    {
        // Constructor.
        public ModelMapsSchema(
            ModelMap<TModel> activeMap,
            IDbContext dbContext)
            : base(activeMap, dbContext, typeof(TModel))
        { }

        // Methods.
        public IModelMapsSchema<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer)
        {
            AddFallbackCustomSerializerHelper(fallbackSerializer);
            return this;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Don't dispose here")]
        public IModelMapsSchema<TModel> AddSecondaryMap(
            string id,
            string? baseModelMapId = null,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
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

        public IModelMapsSchema<TModel> AddSecondaryMap(ModelMap<TModel> modelMap)
        {
            AddSecondaryMapHelper(modelMap);
            return this;
        }
    }
}
