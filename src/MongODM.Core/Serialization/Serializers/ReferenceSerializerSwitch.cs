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

using Etherna.MongODM.Core.Domain.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Serialization.Serializers
{
    public class ReferenceSerializerSwitch<TModel, TKey> :
        SerializerBase<TModel>, IBsonSerializer<TModel>, IBsonDocumentSerializer, IBsonIdProvider, IReferenceContainerSerializer
        where TModel : class, IEntityModel<TKey>
    {
        // Nested classes.
        public class CaseContext
        {
            public SemanticVersion? DocumentVersion { get; set; }
        }

        // Fields.
        private readonly (Func<CaseContext, bool> condition,
            Func<BsonDeserializationContext, BsonDeserializationArgs, TModel> deserializer)[] caseDeserializers;
        private readonly ReferenceSerializer<TModel, TKey> defaultSerializer;

        // Constructor.
        public ReferenceSerializerSwitch(
            ReferenceSerializer<TModel, TKey> defaultSerializer,
            params (Func<CaseContext, bool> condition,
                Func<BsonDeserializationContext, BsonDeserializationArgs, TModel> deserializer)[] caseDeserializers)
        {
            this.caseDeserializers = caseDeserializers ??
                Array.Empty<(Func<CaseContext, bool>, Func<BsonDeserializationContext, BsonDeserializationArgs, TModel>)>();
            this.defaultSerializer = defaultSerializer ?? throw new ArgumentNullException(nameof(defaultSerializer));
        }

        // Properties.
        public IEnumerable<BsonClassMap> ContainedClassMaps => defaultSerializer.ContainedClassMaps;
        public bool? UseCascadeDelete => defaultSerializer.UseCascadeDelete;

        // Methods.
        public override TModel Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            var extendedReader = context.Reader as ExtendedBsonDocumentReader;
            var switchContext = new CaseContext { DocumentVersion = extendedReader?.DocumentVersion };

            // Try cases.
            foreach (var (condition, deserializer) in caseDeserializers)
                if (condition(switchContext))
                    return deserializer(context, args) as TModel;

            // Default.
            return defaultSerializer.Deserialize(context, args);
        }

        public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator) =>
            defaultSerializer.GetDocumentId(document, out id, out idNominalType, out idGenerator);

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TModel value) =>
            defaultSerializer.Serialize(context, args, value);

        public void SetDocumentId(object document, object id) =>
            defaultSerializer.SetDocumentId(document, id);

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo) =>
            defaultSerializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);
    }
}
