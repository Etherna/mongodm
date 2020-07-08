using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Serialization
{
    public class DocumentSchema
    {
        // Constructors.
        public DocumentSchema(
            BsonClassMap classMap,
            Type modelType,
            BsonClassMap? proxyClassMap,
            IBsonSerializer? serializer,
            SemanticVersion version)
        {
            ClassMap = classMap;
            ModelType = modelType;
            ProxyClassMap = proxyClassMap;
            Serializer = serializer;
            Version = version;
        }

        // Properties.
        public BsonClassMap ClassMap { get; }
        public Type ModelType { get; }
        public BsonClassMap? ProxyClassMap { get; }
        public IBsonSerializer? Serializer { get; }
        public SemanticVersion Version { get; }
    }
}
