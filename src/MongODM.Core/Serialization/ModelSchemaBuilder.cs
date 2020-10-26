using Etherna.MongODM.Core.Models;
using Etherna.MongODM.Core.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization
{
    public static class ModelSchemaBuilder
    {
        public static ModelSchema<TModel> GenerateModelSchema<TModel>(
            string id,
            Action<BsonClassMap<TModel>>? classMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null) where TModel : class =>
            new ModelSchema<TModel>(
                id,
                new BsonClassMap<TModel>(classMapInitializer ?? (cm => cm.AutoMap())),
                customSerializer);

        public static void SetDefaultSerializer<TModel>(
            ModelSchema<TModel> modelSchema,
            IDbContext dbContext) where TModel : class
        {
            if (modelSchema is null)
                throw new ArgumentNullException(nameof(modelSchema));
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));
            if (modelSchema.Serializer != null)
                throw new InvalidOperationException("A serializer is already setted");
            if (typeof(TModel).IsAbstract)
                throw new InvalidOperationException("Can't set default serializer of an abstract model");

            modelSchema.Serializer = new ExtendedClassMapSerializer<TModel>(
                dbContext.DbCache,
                dbContext.ApplicationVersion,
                dbContext.SerializerModifierAccessor)
            { AddVersion = typeof(IEntityModel).IsAssignableFrom(typeof(TModel)) }; //true only for entity models
        }

        public static void UseProxyGenerator<TModel>(
            ModelSchema<TModel> modelSchema,
            IDbContext dbContext) where TModel : class
        {
            if (modelSchema is null)
                throw new ArgumentNullException(nameof(modelSchema));
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));
            if (typeof(TModel).IsAbstract)
                throw new InvalidOperationException("Can't generate proxy of an abstract model");

            //set creator
            modelSchema.ModelMap.SetCreator(() => dbContext.ProxyGenerator.CreateInstance(typeof(TModel), dbContext));

            //generate proxy model map
            modelSchema.ProxyModelMap = new BsonClassMap(
                dbContext.ProxyGenerator.CreateInstance(typeof(TModel), dbContext).GetType());
        }
    }
}
