using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization
{
    class ModelSchemaBuilder : IModelSchemaBuilder
    {
        public ModelSchema<TModel> GenerateModelSchema<TModel>(
            string id,
            Action<BsonClassMap<TModel>>? classMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null) where TModel : class =>
            new ModelSchema<TModel>(
                id,
                new BsonClassMap<TModel>(classMapInitializer ?? (cm => cm.AutoMap())),
                customSerializer);
    }
}
