using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Collections.Generic;

namespace Digicando.MongoDM.Serialization.Serializers
{
    public class ExtraElementsSerializer : SerializerBase<object>
    {
        private static ExtraElementsSerializer Instance { get; } = new ExtraElementsSerializer();

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is IDictionary<string, object>)
            {
                context.Writer.WriteStartDocument();
                foreach (var pair in value as IDictionary<string, object>)
                {
                    context.Writer.WriteName(pair.Key);
                    Serialize(context, args, pair.Value);
                }
                context.Writer.WriteEndDocument();
            }
            else if (value is IList<object>)
            {
                context.Writer.WriteStartArray();
                foreach (var element in value as IList<object>)
                {
                    Serialize(context, args, element);
                }
                context.Writer.WriteEndArray();
            }
            else
            {
                BsonSerializer.Serialize(context.Writer, value);
            }
        }

        // Static methods.

        public static TValue DeserializeValue<TValue>(
            object extraElements,
            IBsonSerializer<TValue> serializer = null)
        {
            /* 
             * Must create a context container because arrays
             * can't be serialized on root of documents.
             */
            var document = new BsonDocument();
            var serializationContext = BsonSerializationContext.CreateRoot(new BsonDocumentWriter(document));

            serializationContext.Writer.WriteStartDocument();
            serializationContext.Writer.WriteName("container");
            Instance.Serialize(serializationContext, extraElements);
            serializationContext.Writer.WriteEndDocument();

            // Lookup for a serializer.
            if (serializer == null)
            {
                serializer = BsonSerializer.LookupSerializer<TValue>();
            }

            // Deserialize.
            var deserializationContext = BsonDeserializationContext.CreateRoot(new BsonDocumentReader(document));

            deserializationContext.Reader.ReadStartDocument();
            deserializationContext.Reader.ReadName();
            return serializer.Deserialize(
                deserializationContext,
                new BsonDeserializationArgs { NominalType = typeof(TValue) });
        }
    }
}
