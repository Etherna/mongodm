using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    public interface ISchema : IFreezableConfig
    {
        // Properties.
        IBsonSerializer? ActiveSerializer { get; }
        Type ModelType { get; }
        Type? ProxyModelType { get; }
    }
}