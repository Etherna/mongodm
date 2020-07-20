//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

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
            if (context is null)
                throw new ArgumentNullException(nameof(context));

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
            if (context is null)
                throw new ArgumentNullException(nameof(context));

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
