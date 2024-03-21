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

using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Driver;
using Etherna.MongoDB.Driver.Linq;
using System;
using System.Text;

namespace Etherna.MongODM.Core.FieldDefinition
{
    public class UnmappedFieldDefinition<TDocument> : FieldDefinition<TDocument>
    {
        // Constructor.
        public UnmappedFieldDefinition(
            FieldDefinition<TDocument>? baseDocumentField,
            string unmappedFieldName,
            IBsonSerializer unmappedFieldSerializer)
        {
            if (unmappedFieldName.Contains("."))
                throw new ArgumentException("Field name can't navigate nested documents", nameof(unmappedFieldName));

            BaseDocumentField = baseDocumentField;
            UnmappedFieldName = unmappedFieldName;
            UnmappedFieldSerializer = unmappedFieldSerializer;
        }

        // Properties.
        public FieldDefinition<TDocument>? BaseDocumentField { get; }
        public string UnmappedFieldName { get; }
        public IBsonSerializer UnmappedFieldSerializer { get; }

        // Methods.
        public override RenderedFieldDefinition Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            new(UnmappedFieldDefinitionHelper.BuildFieldPath(BaseDocumentField, UnmappedFieldName, documentSerializer, serializerRegistry, linqProvider),
                UnmappedFieldSerializer);

        public Type? TryGetBaseDocumentType(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            UnmappedFieldDefinitionHelper.TryGetBaseDocumentType(BaseDocumentField, documentSerializer, serializerRegistry);
    }

    public class UnmappedFieldDefinition<TDocument, TField> : FieldDefinition<TDocument, TField>
    {
        // Constructor.
        public UnmappedFieldDefinition(
            FieldDefinition<TDocument>? baseDocumentField,
            string unmappedFieldName,
            IBsonSerializer<TField> unmappedFieldSerializer)
        {
            if (unmappedFieldName.Contains("."))
                throw new ArgumentException("Field name can't navigate nested documents", nameof(unmappedFieldName));

            BaseDocumentField = baseDocumentField;
            UnmappedFieldName = unmappedFieldName;
            UnmappedFieldSerializer = unmappedFieldSerializer;
        }

        // Properties.
        public FieldDefinition<TDocument>? BaseDocumentField { get; }
        public string UnmappedFieldName { get; }
        public IBsonSerializer<TField> UnmappedFieldSerializer { get; }

        // Methods.
        public override RenderedFieldDefinition<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            new(UnmappedFieldDefinitionHelper.BuildFieldPath(BaseDocumentField, UnmappedFieldName, documentSerializer, serializerRegistry, linqProvider),
                UnmappedFieldSerializer,
                UnmappedFieldSerializer,
                UnmappedFieldSerializer);

        public Type? TryGetBaseDocumentType(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            UnmappedFieldDefinitionHelper.TryGetBaseDocumentType(BaseDocumentField, documentSerializer, serializerRegistry);
    }

    internal static class UnmappedFieldDefinitionHelper
    {
        public static string BuildFieldPath<TDocument>(
            FieldDefinition<TDocument>? baseDocumentField,
            string fieldName,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            LinqProvider linqProvider)
        {
            var sb = new StringBuilder();
            if (baseDocumentField is not null)
            {
                var baseDocRenderedField = baseDocumentField.Render(documentSerializer, serializerRegistry, linqProvider);
                sb.Append(baseDocRenderedField.FieldName);
            }
            if (sb.Length > 0)
                sb.Append('.');
            sb.Append(fieldName);

            return sb.ToString();
        }

        public static Type? TryGetBaseDocumentType<TDocument>(
            FieldDefinition<TDocument>? baseDocumentField,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            if (baseDocumentField is null)
                return null;

            var renderedBaseDocumentField = baseDocumentField.Render(documentSerializer, serializerRegistry);
            var baseDocumentFieldSerializer = renderedBaseDocumentField.FieldSerializer;

            // Until serializer is an array serializer, go down to its item serializer.
            var baseDocumentSerializer = baseDocumentFieldSerializer;
            while (baseDocumentSerializer is IBsonArraySerializer arraySerializer)
            {
                if (!arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
                    return null;
                baseDocumentSerializer = itemSerializationInfo.Serializer;
            }

            return baseDocumentSerializer.ValueType;
        }
    }
}
