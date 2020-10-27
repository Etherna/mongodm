using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization
{
    abstract class SchemaConfigurationBase : ISchemaConfiguration
    {
        // Constructor.
        public SchemaConfigurationBase(
            Type modelType,
            bool requireCollectionMigration)
        {
            ModelType = modelType;
            RequireCollectionMigration = requireCollectionMigration;
        }

        // Properties.
        public abstract IBsonSerializer? ActiveSerializer { get; }
        public Type ModelType { get; }
        public abstract Type? ProxyModelType { get; }
        public bool RequireCollectionMigration { get; }
        public abstract bool UseProxyModel { get; }
    }
}
