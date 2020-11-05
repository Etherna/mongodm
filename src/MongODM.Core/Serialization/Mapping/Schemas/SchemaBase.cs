using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    abstract class SchemaBase : FreezableConfig, ISchema
    {
        // Constructor.
        public SchemaBase(
            Type modelType)
        {
            ModelType = modelType;
        }

        // Properties.
        public abstract IBsonSerializer? ActiveSerializer { get; }
        public Type ModelType { get; }
        public abstract Type? ProxyModelType { get; }
        public abstract bool UseProxyModel { get; }
    }
}
