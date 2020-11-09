﻿using MongoDB.Bson.Serialization;
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
            IBsonSerializer<TModel>? customSerializer = null) =>
            AddSecondaryMap(new ModelMap<TModel>(
                id,
                new BsonClassMap<TModel>(modelMapInitializer ?? (cm => cm.AutoMap())),
                DbContext,
                baseModelMapId,
                customSerializer));

        public IModelMapsSchema<TModel> AddSecondaryMap(ModelMap<TModel> modelMap)
        {
            AddSecondaryMapHelper(modelMap);
            return this;
        }
    }
}
