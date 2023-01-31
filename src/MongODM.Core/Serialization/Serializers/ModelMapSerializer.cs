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
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Serialization.Mapping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Serialization.Serializers
{
    public class ModelMapSerializer<TModel> :
        SerializerBase<TModel>,
        IBsonSerializer<TModel>,
        IBsonDocumentSerializer,
        IBsonIdProvider,
        IModelMapsHandlingSerializer
        where TModel : class
    {
        // Fields.
        private IDiscriminatorConvention _discriminatorConvention = default!;
        private readonly IDbContext dbContext;

        // Constructor.
        public ModelMapSerializer(
            IDbContext dbContext)
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));

            this.dbContext = dbContext;
        }

        // Properties.
        public BsonClassMapSerializer<TModel> DefaultBsonClassMapSerializer =>
            (BsonClassMapSerializer<TModel>)dbContext.MapRegistry.GetModelMap(typeof(TModel)).ActiveSchema.BsonClassMap.ToSerializer();

        public IDiscriminatorConvention DiscriminatorConvention
        {
            get
            {
                _discriminatorConvention ??= dbContext.DiscriminatorRegistry.LookupDiscriminatorConvention(typeof(TModel));
                return _discriminatorConvention;
            }
        }

        public IEnumerable<IModelMap> HandledModelMaps => new[] { dbContext.MapRegistry.GetModelMap(typeof(TModel)) };

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
            var actualTypeModelMap = dbContext.MapRegistry.GetModelMap(actualType);

            //deserialize on document
            var bsonDocument = BsonDocumentSerializer.Instance.Deserialize(context, args);

            //get model map id
            string? modelMapId = null;
            if (bsonDocument.TryGetElement(dbContext.Options.ModelMapVersion.ElementName, out BsonElement modelMapIdElement))
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
            if (modelMapId != null && actualTypeModelMap.SchemasById.ContainsKey(modelMapId))
            {
                var task = DeserializeModelMapSchemaHelperAsync(actualTypeModelMap.SchemasById[modelMapId], localContext, args);
                task.Wait();
                model = task.Result;
            }

            //else, if a fallback serializator exists
            else if (actualTypeModelMap.FallbackSerializer != null)
            {
                model = (TModel)actualTypeModelMap.FallbackSerializer.Deserialize(localContext, args);
            }

            //else, if a fallback model map exists
            else if (actualTypeModelMap.FallbackSchema != null)
            {
                var task = DeserializeModelMapSchemaHelperAsync(actualTypeModelMap.FallbackSchema, localContext, args);
                task.Wait();
                model = task.Result;
            }

            //else, deserialize wih current active model map schema
            else
            {
                var task = DeserializeModelMapSchemaHelperAsync(actualTypeModelMap.ActiveSchema, localContext, args);
                task.Wait();
                model = task.Result;
            }

            // Add model to cache (if proxy).
            /* Proxy models enable different features. Anyway, if the model as not been created as a proxy
             * (for example for tests scope) these additional operations are not possible or required.
             * In this case, don't add any not-proxy models in cache.
             */
            if (!dbContext.SerializerModifierAccessor.IsNoCacheEnabled &&
                GetDocumentId(model, out var id, out _, out _) && id != null &&
                dbContext.ProxyGenerator.IsProxyType(model.GetType()))
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
                IsRootDocument = context.Writer is not ExtendedBsonDocumentWriter
            };
            var localContext = BsonSerializationContext.CreateRoot(
                bsonWriter,
                builder => builder.IsDynamicType = context.IsDynamicType);

            // Get default schema.
            var actualType = value.GetType();
            var modelMap = dbContext.MapRegistry.GetModelMap(actualType);

            // Serialize.
            modelMap.ActiveSchema.BsonClassMap.ToSerializer().Serialize(localContext, args, value);

            // Add additional data.
            //add model map id

            /* Verify if already exists, because if current model type is derived from the basic collection type,
             * the basic type serializer is called before, and a more specific serializer as been already invoked
             * from bson class map serializer. In that case, the right model map id is already be setted, and we
             * don't have to replace it with the one wrong of the basic collection model type.
             */
            if (!bsonDocument.Contains(dbContext.Options.ModelMapVersion.ElementName))
                bsonDocument.InsertAt(0, dbContext.MapRegistry.GetActiveModelMapIdBsonElement(actualType));

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

        private static async Task<TModel> DeserializeModelMapSchemaHelperAsync(
            IModelMapSchema modelMapSchema,
            BsonDeserializationContext context,
            BsonDeserializationArgs args)
        {
            /* If is mapped a different serializer than the current one, choose it.
             * Otherwise, if doesn't exist or is already the current, deserialize with BsonClassMap
             */
            var serializer = modelMapSchema.Serializer.GetType() != typeof(ModelMapSerializer<TModel>) ?
                modelMapSchema.Serializer :
                modelMapSchema.BsonClassMap.ToSerializer();

            // If model map schema ask to override the nominal type, override it on args.
            var modelMapSchemaType = modelMapSchema.GetType();
            if (modelMapSchemaType.IsGenericType &&
                modelMapSchemaType.GetGenericTypeDefinition() == typeof(ModelMapSchema<,>))
                args = new BsonDeserializationArgs { NominalType = modelMapSchema.BsonClassMap.ClassType };

            // Deserialize.
            var model = (TModel)serializer.Deserialize(context, args);

            // Fix model.
            return (TModel)await modelMapSchema.FixDeserializedModelAsync(model).ConfigureAwait(false);
        }
    }
}
