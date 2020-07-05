using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Serialization
{
    public class DocumentSchema
    {
        // Constructors.
        public DocumentSchema(BsonClassMap classMap, Type modelType, IBsonSerializer? serializer, SemanticVersion version)
        {
            ClassMap = classMap;
            ModelType = modelType;
            Serializer = serializer;
            Version = version;
        }

        // Properties.
        public BsonClassMap ClassMap { get; }
        public Type ModelType { get; }
        public IBsonSerializer? Serializer { get; }
        public SemanticVersion Version { get; }
    }
}
