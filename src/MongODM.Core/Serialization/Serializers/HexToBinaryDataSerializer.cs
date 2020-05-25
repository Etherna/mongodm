using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace Etherna.MongODM.Serialization.Serializers
{
    public class HexToBinaryDataSerializer : SerializerBase<string>
    {
        public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;
            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Binary:
                    break;
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null!;
                default:
                    var message = $"Expected a value of type Binary, but found a value of type {bsonType} instead.";
                    throw new InvalidOperationException(message);
            }

            var binaryData = bsonReader.ReadBinaryData();
            return BsonUtils.ToHexString(binaryData.Bytes);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
        {
            if (value == null)
            {
                context.Writer.WriteNull();
                return;
            }

            var binaryData = new BsonBinaryData(BsonUtils.ParseHexString(value));
            context.Writer.WriteBinaryData(binaryData);
        }
    }
}
