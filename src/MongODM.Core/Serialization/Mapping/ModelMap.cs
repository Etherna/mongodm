using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Serialization.Serializers;
using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public abstract class ModelMap : FreezableConfig, IModelMap
    {
        // Fields.
        private IBsonSerializer _bsonClassMapSerializer = default!;

        // Constructors.
        protected ModelMap(
            string id,
            string? baseModelMapId,
            BsonClassMap bsonClassMap,
            IBsonSerializer? serializer)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException($"'{nameof(id)}' cannot be null or empty", nameof(id));

            Id = id;
            BaseModelMapId = baseModelMapId;
            BsonClassMap = bsonClassMap ?? throw new ArgumentNullException(nameof(bsonClassMap));
            Serializer = serializer;
        }

        // Properties.
        public string Id { get; }
        public string? BaseModelMapId { get; private set; }
        public BsonClassMap BsonClassMap { get; }
        public IBsonSerializer BsonClassMapSerializer
        {
            get
            {
                if (_bsonClassMapSerializer is null)
                {
                    var classMapSerializerDefinition = typeof(BsonClassMapSerializer<>);
                    var classMapSerializerType = classMapSerializerDefinition.MakeGenericType(ModelType);
                    _bsonClassMapSerializer = (IBsonSerializer)Activator.CreateInstance(classMapSerializerType, BsonClassMap);
                }
                return _bsonClassMapSerializer;
            }
        }
        public bool IsEntity => BsonClassMap.IsEntity();
        public Type ModelType => BsonClassMap.ClassType;
        public IBsonSerializer? Serializer { get; }

        // Methods.
        public void SetBaseModelMap(IModelMap baseModelMap) =>
            ExecuteConfigAction(() =>
            {
                if (baseModelMap is null)
                    throw new ArgumentNullException(nameof(baseModelMap));

                BaseModelMapId = baseModelMap.Id;
                BsonClassMap.SetBaseClassMap(baseModelMap.BsonClassMap);
            });

        public void UseProxyGenerator(IDbContext dbContext) =>
            ExecuteConfigAction(() =>
            {
                if (dbContext is null)
                    throw new ArgumentNullException(nameof(dbContext));
                if (ModelType.IsAbstract)
                    throw new InvalidOperationException("Can't generate proxy of an abstract model");

                // Set creator.
                BsonClassMap.SetCreator(() => dbContext.ProxyGenerator.CreateInstance(ModelType, dbContext));
            });

        // Protected methods.
        protected override void FreezeAction()
        {
            // Freeze bson class maps.
            BsonClassMap.Freeze();
        }

        // Static methods.
        public static IBsonSerializer<TModel> GetDefaultSerializer<TModel>(IDbContext dbContext)
            where TModel : class
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));

            return new ModelMapSerializer<TModel>(
                dbContext.DbCache,
                dbContext.DocumentSemVerOptions,
                dbContext.ModelMapVersionOptions,
                dbContext.SchemaRegister,
                dbContext.SerializerModifierAccessor);
        }
    }

    public class ModelMap<TModel> : ModelMap
    {
        // Constructors.
        public ModelMap(
            string id,
            BsonClassMap<TModel> bsonClassMap,
            string? baseModelMapId = null,
            IBsonSerializer<TModel>? serializer = null)
            : base(id, baseModelMapId, bsonClassMap, serializer)
        { }
    }
}
