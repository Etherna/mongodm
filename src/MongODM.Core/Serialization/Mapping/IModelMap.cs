using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public interface IModelMap : IFreezableConfig
    {
        // Properties.
        string Id { get; }
        string? BaseModelMapId { get; }
        BsonClassMap BsonClassMap { get; }
        IBsonSerializer BsonClassMapSerializer { get; }
        public bool IsEntity { get; }
        public Type ModelType { get; }
        public IBsonSerializer? Serializer { get; }

        // Methods.
        void SetBaseModelMap(IModelMap baseModelMap);
        void UseProxyGenerator(IDbContext dbContext);
    }
}