using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class ReferenceModelMap<TModel> : ModelMapBase
    {
        // Constructors.
        public ReferenceModelMap(
            string id,
            BsonClassMap<TModel> bsonClassMap,
            string? baseModelMapId = null)
            : base(id, baseModelMapId, bsonClassMap, null)
        { }

        // Protected methods.
        protected override IBsonSerializer? GetDefaultSerializer()
        {
            if (typeof(TModel).IsAbstract) //ignore to deserialize abstract types
                return null;

            var classMapSerializerDefinition = typeof(BsonClassMapSerializer<>);
            var classMapSerializerType = classMapSerializerDefinition.MakeGenericType(ModelType);
            var serializer = (IBsonSerializer)Activator.CreateInstance(classMapSerializerType, BsonClassMap);

            return serializer;
        }
    }
}
