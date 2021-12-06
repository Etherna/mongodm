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
using Etherna.MongoDB.Bson.Serialization.Conventions;
using Etherna.MongoDB.Bson.Serialization.Serializers;
using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.ProxyModels;
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
        private IDiscriminatorConvention _discriminatorConvention = default!;
        private readonly BsonElement documentSemVerElement;
        private readonly IDbContext dbContext;
        private readonly DocumentSemVerOptions documentSemVerOptions;
        private readonly ModelMapVersionOptions modelMapVersionOptions;

        // Constructor.
        public ModelMapSerializer(
            IDbContext dbContext,
            DocumentSemVerOptions? overrideDocumentSemVerOptions = null,
            ModelMapVersionOptions? overrideModelMapVersionOptions = null)
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));

            this.dbContext = dbContext;

            documentSemVerOptions = overrideDocumentSemVerOptions ?? dbContext.Options.DocumentSemVer;
            modelMapVersionOptions = overrideModelMapVersionOptions ?? dbContext.Options.ModelMapVersion;

            documentSemVerElement = new BsonElement(
                documentSemVerOptions.ElementName,
                documentSemVerOptions.CurrentVersion.ToBsonArray());
        }

        // Properties.
        public IEnumerable<BsonClassMap> AllChildClassMaps => dbContext.SchemaRegister.GetModelMapsSchema(typeof(TModel))
            .AllMapsDictionary.Values.Select(map => map.BsonClassMap);

        public BsonClassMapSerializer<TModel> DefaultBsonClassMapSerializer =>
            (BsonClassMapSerializer<TModel>)dbContext.SchemaRegister.GetModelMapsSchema(typeof(TModel)).ActiveMap.BsonClassMapSerializer;

        public IDiscriminatorConvention DiscriminatorConvention
        {
            get
            {
                if (_discriminatorConvention == null)
                    _discriminatorConvention = dbContext.DiscriminatorRegister.LookupDiscriminatorConvention(typeof(TModel));
                return _discriminatorConvention;
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
            //get actual type and schema
            var actualType = DiscriminatorConvention.GetActualType(context.Reader, args.NominalType);
            var actualTypeSchema = dbContext.SchemaRegister.GetModelMapsSchema(actualType);

            //deserialize on document
            var bsonDocument = BsonDocumentSerializer.Instance.Deserialize(context, args);

            //get version
            SemanticVersion? documentSemVer = null;
            if (bsonDocument.TryGetElement(documentSemVerOptions.ElementName, out BsonElement versionElement))
            {
                documentSemVer = BsonValueToSemVer(versionElement.Value);
                bsonDocument.RemoveElement(versionElement); //don't report into extra elements
            }

            //get model map id
            string? modelMapId = null;
            if (bsonDocument.TryGetElement(modelMapVersionOptions.ElementName, out BsonElement modelMapIdElement))
            {
                modelMapId = BsonValueToModelMapId(modelMapIdElement.Value);
                bsonDocument.RemoveElement(modelMapIdElement); //don't report into extra elements
            }

            // Initialize localContext.
            using var bsonReader = new BsonDocumentReader(bsonDocument);
            var localContext = BsonDeserializationContext.CreateRoot(bsonReader, builder =>
            {
                builder.AllowDuplicateElementNames = context.AllowDuplicateElementNames;
                builder.DynamicArraySerializer = context.DynamicArraySerializer;
                builder.DynamicDocumentSerializer = context.DynamicDocumentSerializer;
            });

            // Deserialize.
            TModel model;

            //if a correct model map is identified with its id
            if (modelMapId != null && actualTypeSchema.AllMapsDictionary.ContainsKey(modelMapId))
            {
                var serializer = actualTypeSchema.AllMapsDictionary[modelMapId].BsonClassMapSerializer;
                model = (TModel)serializer.Deserialize(localContext, args);
            }

            //else, if a fallback serializator exists
            else if (actualTypeSchema.FallbackSerializer != null)
            {
                model = (TModel)actualTypeSchema.FallbackSerializer.Deserialize(localContext, args);
            }

            //else, deserialize wih current active model map
            else
            {
                model = (TModel)actualTypeSchema.ActiveMap.BsonClassMapSerializer.Deserialize(localContext, args);
            }

            // Add model to cache.
            if (!dbContext.SerializerModifierAccessor.IsNoCacheEnabled &&
                GetDocumentId(model, out var id, out _, out _) && id != null)
            {
                if (dbContext.DbCache.LoadedModels.ContainsKey(id))
                {
                    var fullModel = model;
                    model = (TModel)dbContext.DbCache.LoadedModels[id];

                    if (((IReferenceable)model).IsSummary)
                        ((IReferenceable)model).MergeFullModel(fullModel);
                }
                else
                {
                    dbContext.DbCache.AddModel(id, (IEntityModel)model);
                }
            }

            // Enable auditing.
            (model as IAuditable)?.EnableAuditing();

            return model;
        }

        public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator) =>
            DefaultBsonClassMapSerializer.GetDocumentId(document, out id, out idNominalType, out idGenerator);

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

            // Get default schema.
            var actualType = value.GetType();
            var modelMapsSchema = dbContext.SchemaRegister.GetModelMapsSchema(actualType);

            // Serialize.
            modelMapsSchema.ActiveBsonClassMapSerializer.Serialize(localContext, args, value);

            // Add additional data.
            //add model map id

            /* Verify if already exists, because if current model type is derived from the basic collection type,
             * the basic type serializer is called before, and a more specific serializer as been already invoked
             * from bson class map serializer. In that case, the right model map id is already be setted, and we
             * don't have to replace it with the one wrong of the basic collection model type.
             */
            if (!bsonDocument.Contains(modelMapVersionOptions.ElementName))
                bsonDocument.InsertAt(0, dbContext.SchemaRegister.GetActiveModelMapIdBsonElement(actualType));

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
            DefaultBsonClassMapSerializer.SetDocumentId(document, id);

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo) =>
            DefaultBsonClassMapSerializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);

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
