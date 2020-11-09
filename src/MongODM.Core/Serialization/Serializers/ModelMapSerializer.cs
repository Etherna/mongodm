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
using Etherna.MongODM.Core.Options;
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
        SerializerBase<TModel>, IBsonSerializer<TModel>, IBsonDocumentSerializer, IBsonIdProvider, IModelMapsContainerSerializer
        where TModel : class
    {
        // Fields.
        private IReadOnlyDictionary<string, BsonClassMapSerializer<TModel>> _classMapSerializers = default!;
        private IModelMapsSchema<TModel> _modelMapsSchema = default!;
        private readonly IDbCache dbCache;
        private readonly BsonElement documentSemVerElement;
        private readonly DocumentSemVerOptions documentSemVerOptions;
        private readonly ModelMapVersionOptions modelMapVersionOptions;
        private readonly ISchemaRegister schemaRegister;
        private readonly ISerializerModifierAccessor serializerModifierAccessor;

        // Constructor.
        public ModelMapSerializer(
            IDbCache dbCache,
            DocumentSemVerOptions documentSemVerOptions,
            ModelMapVersionOptions modelMapVersionOptions,
            ISchemaRegister schemaRegister,
            ISerializerModifierAccessor serializerModifierAccessor)
        {
            this.dbCache = dbCache ?? throw new ArgumentNullException(nameof(dbCache));
            this.documentSemVerOptions = documentSemVerOptions ?? throw new ArgumentNullException(nameof(documentSemVerOptions));
            this.modelMapVersionOptions = modelMapVersionOptions ?? throw new ArgumentNullException(nameof(modelMapVersionOptions));
            this.schemaRegister = schemaRegister ?? throw new ArgumentNullException(nameof(schemaRegister));
            this.serializerModifierAccessor = serializerModifierAccessor ?? throw new ArgumentNullException(nameof(serializerModifierAccessor));

            documentSemVerElement = new BsonElement(
                documentSemVerOptions.ElementName,
                documentSemVerOptions.CurrentVersion.ToBsonArray());
        }

        // Properties.
        public BsonClassMapSerializer<TModel> ActiveClassMapSerializer => ClassMapSerializers[ModelMapsSchema.ActiveMap.Id];

        public IEnumerable<BsonClassMap> AllChildClassMaps => ModelMapsSchema.AllMapsDictionary.Values.Select(map => map.BsonClassMap);

        public IReadOnlyDictionary<string, BsonClassMapSerializer<TModel>> ClassMapSerializers
        {
            get
            {
                if (_classMapSerializers is null)
                {
                    _classMapSerializers = ModelMapsSchema.AllMapsDictionary.ToDictionary(
                        pair => pair.Key,
                        pair =>
                        {
                            var classMap = pair.Value.BsonClassMap;
                            return new BsonClassMapSerializer<TModel>(classMap);
                        });
                }

                return _classMapSerializers;
            }
        }

        public IModelMapsSchema<TModel> ModelMapsSchema
        {
            get
            {
                if (_modelMapsSchema is null)
                    _modelMapsSchema = schemaRegister.GetModelMapsSchema<TModel>();

                return _modelMapsSchema;
            }
        }

        // Methods.
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

            // Find pre-deserialization informations.
            //deserialize on document
            var bsonDocument = BsonDocumentSerializer.Instance.Deserialize(context, args);

            //get version
            SemanticVersion? documentSemVer = null;
            if (bsonDocument.TryGetElement(documentSemVerOptions.ElementName, out BsonElement versionElement))
                documentSemVer = BsonValueToSemVer(versionElement.Value);

            //get model map id
            string? modelMapId = null;
            if (bsonDocument.TryGetElement(modelMapVersionOptions.ElementName, out BsonElement modelMapIdElement))
                modelMapId = ModelMapSerializer<TModel>.BsonValueToModelMapId(modelMapIdElement.Value);

            // Initialize localContext and bsonReader
            using var bsonReader = new ExtendedBsonDocumentReader(bsonDocument)
            {
                DocumentSemVer = documentSemVer
            };
            var localContext = BsonDeserializationContext.CreateRoot(bsonReader, builder =>
            {
                builder.AllowDuplicateElementNames = context.AllowDuplicateElementNames;
                builder.DynamicArraySerializer = context.DynamicArraySerializer;
                builder.DynamicDocumentSerializer = context.DynamicDocumentSerializer;
            });

            // Deserialize.
            TModel model;

            //if a correct model map is identified with its id
            if (modelMapId != null && ClassMapSerializers.ContainsKey(modelMapId))
            {
                var serializer = ClassMapSerializers[modelMapId];
                model = serializer.Deserialize(localContext, args);
            }

            //else, if a fallback serializator exists
            else if (ModelMapsSchema.FallbackSerializer != null)
            {
                model = (TModel)ModelMapsSchema.FallbackSerializer.Deserialize(localContext, args);
            }

            //else, deserialize wih current active model map
            else
            {
                model = ActiveClassMapSerializer.Deserialize(localContext, args);
            }

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
            ActiveClassMapSerializer.GetDocumentId(document, out id, out idNominalType, out idGenerator);

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

            // Clear extra elements.
            if (value is IModel model)
                model.ExtraElements?.Clear();

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
            ActiveClassMapSerializer.Serialize(localContext, args, value);

            // Add additional data.
            //add model map id
            if (bsonDocument.Contains(modelMapVersionOptions.ElementName))
                bsonDocument.Remove(modelMapVersionOptions.ElementName);
            bsonDocument.InsertAt(0, new BsonElement(
                modelMapVersionOptions.ElementName,
                new BsonString(ModelMapsSchema.ActiveMap.Id)));

            //add version
            if (documentSemVerOptions.WriteInDocuments && bsonWriter.IsRootDocument)
            {
                if (bsonDocument.Contains(documentSemVerOptions.ElementName))
                    bsonDocument.Remove(documentSemVerOptions.ElementName);
                bsonDocument.InsertAt(1, documentSemVerElement);
            }

            // Serialize document.
            BsonDocumentSerializer.Instance.Serialize(context, args, bsonDocument);
        }

        public void SetDocumentId(object document, object id) =>
            ActiveClassMapSerializer.SetDocumentId(document, id);

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo) =>
            ActiveClassMapSerializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);

        // Helpers.
        private static string? BsonValueToModelMapId(BsonValue bsonValue) =>
            bsonValue switch
            {
                BsonNull _ => null,
                BsonString bsonString => bsonString.AsString,
                _ => throw new NotSupportedException(),
            };

        private static SemanticVersion? BsonValueToSemVer(BsonValue bsonValue) =>
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
    }
}
