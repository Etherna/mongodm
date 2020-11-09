using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public abstract class ModelMapBase : FreezableConfig
    {
        // Fields.
        private IBsonSerializer _bsonClassMapSerializer = default!;
        private IBsonSerializer? _serializer;

        // Constructors.
        protected ModelMapBase(
            string id,
            string? baseModelMapId,
            BsonClassMap bsonClassMap,
            IBsonSerializer? serializer)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            BaseModelMapId = baseModelMapId;
            BsonClassMap = bsonClassMap ?? throw new ArgumentNullException(nameof(bsonClassMap));
            _serializer = serializer;
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
        public IBsonSerializer? Serializer
        {
            get
            {
                if (_serializer is null && !ModelType.IsAbstract) //can't set serializer of an abstract model
                    _serializer = GetDefaultSerializer();

                return _serializer;
            }
        }

        // Methods.
        public void SetBaseModelMap(ModelMapBase baseModelMap) =>
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

        protected virtual IBsonSerializer? GetDefaultSerializer() => default;
    }
}
