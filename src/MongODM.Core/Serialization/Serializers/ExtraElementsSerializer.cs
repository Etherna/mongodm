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

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.IO;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Serialization.Serializers
{
    /// <summary>
    /// Utility serializer used for help into document migration scripts.
    /// </summary>
    public class ExtraElementsSerializer : SerializerBase<object>
    {
        // Fields.
        private readonly IDbContext dbContext;

        // Constructor.
        public ExtraElementsSerializer(IDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // Methods.
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            if (value is IDictionary<string, object> dictionary)
            {
                context.Writer.WriteStartDocument();
                foreach (var pair in dictionary)
                {
                    context.Writer.WriteName(pair.Key);
                    Serialize(context, args, pair.Value);
                }
                context.Writer.WriteEndDocument();
            }
            else if (value is IList<object> list)
            {
                context.Writer.WriteStartArray();
                foreach (var element in list)
                {
                    Serialize(context, args, element);
                }
                context.Writer.WriteEndArray();
            }
            else
            {
                var serializer = dbContext.SerializerRegistry.GetSerializer<object>();
                serializer.Serialize(context, value);
            }
        }

        public TValue DeserializeValue<TValue>(
            object extraElements,
            IBsonSerializer<TValue>? serializer = null)
        {
            /* 
             * Must create a context container because arrays
             * can't be serialized on root of documents.
             */
            var document = new BsonDocument();
            using var documentWriter = new BsonDocumentWriter(document);
            var serializationContext = BsonSerializationContext.CreateRoot(documentWriter);

            serializationContext.Writer.WriteStartDocument();
            serializationContext.Writer.WriteName("container");
            this.Serialize(serializationContext, extraElements);
            serializationContext.Writer.WriteEndDocument();

            // Lookup for a serializer.
            serializer ??= dbContext.SerializerRegistry.GetSerializer<TValue>();

            // Deserialize.
            using var documentReader = new BsonDocumentReader(document);
            var deserializationContext = BsonDeserializationContext.CreateRoot(documentReader);

            deserializationContext.Reader.ReadStartDocument();
            deserializationContext.Reader.ReadName();
            return serializer.Deserialize(
                deserializationContext,
                new BsonDeserializationArgs { NominalType = typeof(TValue) });
        }
    }
}
