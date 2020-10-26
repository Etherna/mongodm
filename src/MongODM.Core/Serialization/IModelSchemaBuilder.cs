using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization
{
    public interface IModelSchemaBuilder
    {
        ModelSchema<TModel> GenerateModelSchema<TModel>(
            string id,
            Action<BsonClassMap<TModel>>? classMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null) where TModel : class;
    }
}