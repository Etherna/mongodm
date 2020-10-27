using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization
{
    public interface ISchemaConfiguration
    {
        IBsonSerializer? ActiveSerializer { get; }
        Type ModelType { get; }
        Type? ProxyModelType { get; }
        bool RequireCollectionMigration { get; }
        bool UseProxyModel { get; }
    }
}