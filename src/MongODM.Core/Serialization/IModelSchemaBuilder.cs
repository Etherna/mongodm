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

        void SetDefaultSerializer<TModel>(
            ModelSchema<TModel> modelSchema,
            IDbContext dbContext) where TModel : class;

        void UseProxyGenerator<TModel>(
            ModelSchema<TModel> modelSchema,
            IDbContext dbContext) where TModel : class;
    }
}