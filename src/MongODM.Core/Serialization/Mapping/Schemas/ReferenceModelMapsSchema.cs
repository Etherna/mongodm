using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    class ReferenceModelMapsSchema<TModel> : ModelMapsSchemaBase, IReferenceModelMapsSchemaBuilder<TModel>
    {
        // Constructor.
        public ReferenceModelMapsSchema(
            ModelMap<TModel> activeMap,
            IDbContext dbContext)
            : base(activeMap, dbContext, typeof(TModel))
        { }

        // Methods.
        public IReferenceModelMapsSchemaBuilder<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer)
        {
            AddFallbackCustomSerializerHelper(fallbackSerializer);
            return this;
        }

        public IReferenceModelMapsSchemaBuilder<TModel> AddSecondaryMap(
            string id,
            string? baseModelMapId = null,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null) =>
            AddSecondaryMap(new ModelMap<TModel>(
                id,
                new BsonClassMap<TModel>(modelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId));

        public IReferenceModelMapsSchemaBuilder<TModel> AddSecondaryMap(ModelMap<TModel> modelMap)
        {
            AddSecondaryMapHelper(modelMap);
            return this;
        }
    }
}
