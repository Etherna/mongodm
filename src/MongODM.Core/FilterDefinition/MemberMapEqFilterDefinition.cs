// Copyright 2020-present Etherna SA
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.IO;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Driver;
using Etherna.MongoDB.Driver.Linq;
using Etherna.MongODM.Core.Exceptions;
using Etherna.MongODM.Core.FieldDefinition;
using Etherna.MongODM.Core.Serialization.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Etherna.MongODM.Core.FilterDefinition
{
    public class MemberMapEqFilterDefinition<TDocument, TItem> : FilterDefinition<TDocument>
    {
        private const string ElemMatchCommand = "$elemMatch";

        // Fields.
        private readonly IMemberMap memberMap;
        private readonly TItem value;

        // Constructor.
        public MemberMapEqFilterDefinition(
            IMemberMap memberMap,
            TItem value)
        {
            ArgumentNullException.ThrowIfNull(memberMap, nameof(memberMap));
            
            if (memberMap.ElementPathHasUndefinedDocumentElement)
                throw new ArgumentException("Can't create filter with member map path having undefined document elements");

            this.memberMap = memberMap;
            this.value = value;
        }

        // Methods.
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            var memberMapFieldDefinition = new MemberMapFieldDefinition<TDocument, TItem>(
                memberMap,
                _ => $".{ElemMatchCommand}",
                _ => throw new MongodmElementPathRenderingException());
            var renderedField = memberMapFieldDefinition.Render(documentSerializer, serializerRegistry, linqProvider);
            var segmentedField = renderedField.FieldName.Split('.');
            var filterDocument = BuildBsonDocument(segmentedField, value, renderedField.ValueSerializer);
            return filterDocument;
        }

        // Helpers.
        private static BsonDocument BuildBsonDocument(IEnumerable<string> segmentedField, TItem value, IBsonSerializer<TItem> valueSerializer)
        {
            // Recursion building elemMatch filters.
            var sb = new StringBuilder();
            foreach ( var (fieldSegment, i) in segmentedField.Select((f, i) => (f, i)))
            {
                if (fieldSegment == ElemMatchCommand)
                    return sb.Length == 0 ?
                        new BsonDocument(ElemMatchCommand, BuildBsonDocument(segmentedField.Skip(i + 1), value, valueSerializer)) :
                        new BsonDocument(sb.ToString(), new BsonDocument(ElemMatchCommand, BuildBsonDocument(segmentedField.Skip(i + 1), value, valueSerializer)));
                else
                    sb.Append((sb.Length == 0 ? "" : ".") + fieldSegment);
            }

            // Exit building eq filter.
            var eqDocument = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(eqDocument))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(sb.ToString());
                valueSerializer.Serialize(context, value);
                bsonWriter.WriteEndDocument();
            }
            return eqDocument;
        }
    }
}
