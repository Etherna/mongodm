using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Serializers.Config
{
    public abstract class ReferenceModelMap : FreezableConfig
    {
        // Fields.
        private readonly BsonClassMap _bsonClassMap;

        // Constructors.
        protected ReferenceModelMap(
            string id,
            BsonClassMap bsonClassMap)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            _bsonClassMap = bsonClassMap ?? throw new ArgumentNullException(nameof(bsonClassMap));
        }

        // Properties.
        public string Id { get; }
        public BsonClassMap BsonClassMap
        {
            get
            {
                Freeze();
                return _bsonClassMap;
            }
        }
        public bool IsEntity => BsonClassMap.IsEntity();
        public Type ModelType => BsonClassMap.ClassType;

        // Methods.
        internal void UseProxyGenerator(IDbContext dbContext) =>
            ExecuteConfigAction(() =>
            {
                if (dbContext is null)
                    throw new ArgumentNullException(nameof(dbContext));
                if (ModelType.IsAbstract)
                    throw new InvalidOperationException("Can't generate proxy of an abstract model");

                // Set creator.
                _bsonClassMap.SetCreator(() => dbContext.ProxyGenerator.CreateInstance(ModelType, dbContext));
            });

        // Protected methods.
        protected override void FreezeAction()
        {
            _bsonClassMap.Freeze();
        }
    }

    public class ReferenceModelMap<TModel> : ReferenceModelMap
    {
        // Constructors.
        public ReferenceModelMap(
            string id,
            BsonClassMap<TModel> bsonClassMap)
            : base(id, bsonClassMap)
        { }
    }
}
