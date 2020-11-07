using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    public abstract class SchemaBase : FreezableConfig, ISchema
    {
        // Constructor.
        protected SchemaBase(
            Type modelType)
        {
            ModelType = modelType;
        }

        // Properties.
        public abstract IBsonSerializer? ActiveSerializer { get; }
        public Type ModelType { get; }
        public abstract Type? ProxyModelType { get; }
    }
}
