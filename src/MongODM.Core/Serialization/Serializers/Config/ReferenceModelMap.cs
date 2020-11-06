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
            string? baseModelMapId,
            BsonClassMap bsonClassMap)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            BaseModelMapId = baseModelMapId;
            _bsonClassMap = bsonClassMap ?? throw new ArgumentNullException(nameof(bsonClassMap));
        }

        // Properties.
        public string Id { get; }
        public string? BaseModelMapId { get; private set; }
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
        public void SetBaseModelMap(ReferenceModelMap baseModelMap) =>
            ExecuteConfigAction(() =>
            {
                if (baseModelMap is null)
                    throw new ArgumentNullException(nameof(baseModelMap));

                BaseModelMapId = baseModelMap.Id;
                _bsonClassMap.SetBaseClassMap(baseModelMap._bsonClassMap);
            });

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
            BsonClassMap<TModel> bsonClassMap,
            string? baseModelMapId = null)
            : base(id, baseModelMapId, bsonClassMap)
        { }
    }
}
