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
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Mapping.Schemas;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Serializers
{
    public class ModelMapSerializer<TModel> :
        SerializerBase<TModel>, IBsonSerializer<TModel>, IBsonDocumentSerializer, IBsonIdProvider, IModelMapsSchemaSerializer
        where TModel : class
    {
        // Nested struct.
        private struct ExtraElementCondition
        {
            public BsonElement Element { get; set; }
            public Func<BsonSerializationContext, bool> Condition { get; set; }
        }

        // Static readonly fields.
        private readonly BsonElement documentVersionElement;

        // Fields.
        private readonly IDbCache dbCache;
        private readonly ISerializerModifierAccessor serializerModifierAccessor;
        private readonly ISchemaRegister schemaRegister;
        private readonly ICollection<ExtraElementCondition> extraElements;
        private BsonClassMapSerializer<TModel> _serializer = default!;

        // Constructor.
        public ModelMapSerializer(
            IDbCache dbCache,
            SemanticVersion documentVersion,
            ISerializerModifierAccessor serializerModifierAccessor,
            ISchemaRegister schemaRegister)
        {
            if (documentVersion is null)
                throw new ArgumentNullException(nameof(documentVersion));

            this.dbCache = dbCache ?? throw new ArgumentNullException(nameof(dbCache));
            this.serializerModifierAccessor = serializerModifierAccessor ?? throw new ArgumentNullException(nameof(serializerModifierAccessor));
            this.schemaRegister = schemaRegister ?? throw new ArgumentNullException(nameof(schemaRegister));
            extraElements = new List<ExtraElementCondition>();
            documentVersionElement = new BsonElement(
                DbContext.DocumentVersionElementName,
                DocumentVersionToBsonArray(documentVersion));
        }

        // Properties.
        public bool AddVersion { get; set; }
        public IModelMapsSchema ModelMapsSchema => (IModelMapsSchema)schemaRegister.Schemas[typeof(TModel)];
        public BsonClassMapSerializer<TModel> Serializer
        {
            get
            {
                if (_serializer == null)
                {
                    var classMap = ModelMapsSchema.ActiveMap.BsonClassMap;
                    _serializer = new BsonClassMapSerializer<TModel>(classMap);
                }
                return _serializer;
            }
        }

        // Methods.
        public ModelMapSerializer<TModel> AddExtraElement(
            BsonElement element,
            Func<BsonSerializationContext, bool>? condition = null)
        {
            extraElements.Add(new ExtraElementCondition
            {
                Element = element,
                Condition = condition ?? (_ => true)
            });
            return this;
        }

        public override TModel Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            // Check if null.
            if (context.Reader.CurrentBsonType == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null!;
            }

            // Deserialize on document.
            var bsonDocument = BsonDocumentSerializer.Instance.Deserialize(context, args);

            // Get version.
            SemanticVersion? documentVersion = null;
            if (bsonDocument.TryGetElement(DbContext.DocumentVersionElementName, out BsonElement versionElement))
                documentVersion = BsonValueToDocumentVersion(versionElement.Value);

            // Initialize localContext and bsonReader
            using var bsonReader = new ExtendedBsonDocumentReader(bsonDocument)
            {
                DocumentVersion = documentVersion
            };
            var localContext = BsonDeserializationContext.CreateRoot(bsonReader, builder =>
            {
                builder.AllowDuplicateElementNames = context.AllowDuplicateElementNames;
                builder.DynamicArraySerializer = context.DynamicArraySerializer;
                builder.DynamicDocumentSerializer = context.DynamicDocumentSerializer;
            });

            // Deserialize.
            var model = Serializer.Deserialize(localContext, args);

            // Add model to cache.
            if (!serializerModifierAccessor.IsNoCacheEnabled &&
                GetDocumentId(model, out var id, out _, out _) && id != null)
            {
                if (dbCache.LoadedModels.ContainsKey(id))
                {
                    var fullModel = model;
                    model = (TModel)dbCache.LoadedModels[id];

                    if (((IReferenceable)model).IsSummary)
                        ((IReferenceable)model).MergeFullModel(fullModel);
                }
                else
                {
                    dbCache.AddModel(id, (IEntityModel)model);
                }
            }

            // Enable auditing.
            (model as IAuditable)?.EnableAuditing();

            return model;
        }

        public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator) =>
            Serializer.GetDocumentId(document, out id, out idNominalType, out idGenerator);

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TModel value)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            // Serialize null object.
            if (value == null)
            {
                context.Writer.WriteNull();
                return;
            }

            // Initialize localContext, bsonDocument and bsonWriter.
            var bsonDocument = new BsonDocument();
            using var bsonWriter = new ExtendedBsonDocumentWriter(bsonDocument)
            {
                IsRootDocument = !(context.Writer is ExtendedBsonDocumentWriter)
            };
            var localContext = BsonSerializationContext.CreateRoot(
                bsonWriter,
                builder => builder.IsDynamicType = context.IsDynamicType);

            // Serialize.
            Serializer.Serialize(localContext, args, value);

            // Add version.
            if (AddVersion && bsonWriter.IsRootDocument)
            {
                if (bsonDocument.Contains(documentVersionElement.Name))
                    bsonDocument.Remove(documentVersionElement.Name);
                bsonDocument.InsertAt(0, documentVersionElement);
            }

            // Add extra elements.
            foreach (var element in from elementCondition in extraElements
                                    where elementCondition.Condition(localContext)
                                    select elementCondition.Element)
            {
                if (bsonDocument.Contains(element.Name))
                    bsonDocument.Remove(element.Name);
                bsonDocument.Add(element);
            }

            // Serialize document.
            BsonDocumentSerializer.Instance.Serialize(context, args, bsonDocument);
        }

        public void SetDocumentId(object document, object id) =>
            Serializer.SetDocumentId(document, id);

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo) =>
            Serializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);

        // Helpers.
        private static SemanticVersion? BsonValueToDocumentVersion(BsonValue bsonValue) =>
            bsonValue switch
            {
                BsonNull _ => null,
                BsonString bsonString => new SemanticVersion(bsonString.AsString),
                BsonArray bsonArray => new SemanticVersion(
                    bsonArray[0].AsInt32,
                    bsonArray[1].AsInt32,
                    bsonArray[2].AsInt32,
                    bsonArray.Count >= 4 ? bsonArray[3].AsString : null),
                _ => throw new NotSupportedException(),
            };

        private static BsonArray DocumentVersionToBsonArray(SemanticVersion documentVersion)
        {
            var bsonArray = new BsonArray(new[]
            {
                new BsonInt32(documentVersion.MajorRelease),
                new BsonInt32(documentVersion.MinorRelease),
                new BsonInt32(documentVersion.PatchRelease)
            });
            if (documentVersion.LabelRelease != null)
            {
                bsonArray.Add(new BsonString(documentVersion.LabelRelease));
            }

            return bsonArray;
        }
    }
}
