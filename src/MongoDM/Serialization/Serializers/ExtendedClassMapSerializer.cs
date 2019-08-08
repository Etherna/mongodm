using Digicando.DomainHelper;
using Digicando.MongoDM.Models;
using Digicando.MongoDM.ProxyModels;
using Digicando.MongoDM.Serialization.Modifiers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Digicando.MongoDM.Serialization.Serializers
{
    public class ExtendedClassMapSerializer<TModel> :
        SerializerBase<TModel>, IBsonSerializer<TModel>, IBsonDocumentSerializer, IBsonIdProvider, IClassMapContainerSerializer
        where TModel : class
    {
        // Nested struct.
        private struct ExtraElementCondition
        {
            Func<BsonSerializationContext, bool> _condition;

            public BsonElement Element { get; set; }
            public Func<BsonSerializationContext, bool> Condition
            {
                get { return _condition ?? (_ => true); }
                set { _condition = value; }
            }
        }

        // Static readonly fields.
        private readonly BsonElement documentVersionElement;

        // Fields.
        private BsonClassMapSerializer<TModel> _serializer;
        private readonly IDBContextBase dbContext;
        private readonly ISerializerModifierAccessor serializerModifierAccessor;
        private readonly ICollection<ExtraElementCondition> extraElements;
        private readonly Func<TModel, DocumentVersion, Task<TModel>> fixDeserializedModelAsync;

        // Constructor.
        public ExtendedClassMapSerializer(
            IDBContextBase dbContext,
            ISerializerModifierAccessor serializerModifierAccessor,
            Func<TModel, DocumentVersion, Task<TModel>> fixDeserializedModelAsync = null)
        {
            this.dbContext = dbContext;
            this.serializerModifierAccessor = serializerModifierAccessor;
            extraElements = new List<ExtraElementCondition>();
            this.fixDeserializedModelAsync = fixDeserializedModelAsync ?? ((m, _) => Task.FromResult(m));
            documentVersionElement = new BsonElement(
                DBContextBase.DocumentVersionElementName,
                DocumentVersionToBsonArray(dbContext.DocumentVersion));
        }

        // Properties.
        public bool AddVersion { get; set; }
        public IEnumerable<BsonClassMap> ContainedClassMaps => new[] { BsonClassMap.LookupClassMap(typeof(TModel)) };

        // Methods.
        public ExtendedClassMapSerializer<TModel> AddExtraElement(
            BsonElement element,
            Func<BsonSerializationContext, bool> condition = null)
        {
            extraElements.Add(new ExtraElementCondition
            {
                Element = element,
                Condition = condition
            });
            return this;
        }

        public override TModel Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            // Check if null.
            if (context.Reader.CurrentBsonType == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }

            // Deserialize on document.
            var bsonDocument = BsonDocumentSerializer.Instance.Deserialize(context, args);

            // Get version.
            DocumentVersion documentVersion = null;
            if (bsonDocument.TryGetElement(DBContextBase.DocumentVersionElementName, out BsonElement versionElement))
                documentVersion = BsonValueToDocumentVersion(versionElement.Value);

            // Initialize localContext and bsonReader
            var bsonReader = new ExtendedBsonDocumentReader(bsonDocument)
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
            var serializer = GetSerializer();
            var model = serializer.Deserialize(localContext, args);

            // Add model to cache.
            if (!serializerModifierAccessor.IsNoCacheEnabled &&
                GetDocumentId(model, out var id, out _, out _) && id != null)
            {
                if (dbContext.DBCache.LoadedModels.ContainsKey(id))
                {
                    var fullModel = model;
                    model = dbContext.DBCache.LoadedModels[id] as TModel;

                    if ((model as IReferenceable).IsSummary)
                        (model as IReferenceable).MergeFullModel(fullModel);
                }
                else
                {
                    dbContext.DBCache.AddModel(id, model as IEntityModel);
                }
            }

            // Fix model.
            var task = fixDeserializedModelAsync(model, documentVersion);
            task.Wait();
            model = task.Result;

            // Enable auditing.
            (model as IAuditable)?.EnableAuditing();

            return model;
        }

        public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            var serializer = GetSerializer();
            return serializer.GetDocumentId(document, out id, out idNominalType, out idGenerator);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TModel value)
        {
            // Serialize null object.
            if (value == null)
            {
                context.Writer.WriteNull();
                return;
            }

            // Initialize localContext, bsonDocument and bsonWriter.
            var bsonDocument = new BsonDocument();
            var bsonWriter = new ExtendedBsonDocumentWriter(bsonDocument)
            {
                IsRootDocument = !(context.Writer is ExtendedBsonDocumentWriter)
            };
            var localContext = BsonSerializationContext.CreateRoot(
                bsonWriter,
                builder => builder.IsDynamicType = context.IsDynamicType);

            // Purify model from proxy class.
            if (value.GetType() != typeof(TModel))
            {
                var constructor = typeof(TModel).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null) ??
                    typeof(TModel).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null);
                var newModel = (TModel)constructor.Invoke(new object[0]);
                ReflectionHelper.CloneModel(value, newModel);
                value = newModel;
            }

            // Serialize.
            var serializer = GetSerializer();
            serializer.Serialize(localContext, args, value);

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

        public void SetDocumentId(object document, object id)
        {
            var serializer = GetSerializer();
            serializer.SetDocumentId(document, id);
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            var serializer = GetSerializer();
            return serializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);
        }

        // Helpers.
        private static DocumentVersion BsonValueToDocumentVersion(BsonValue bsonValue)
        {
            switch (bsonValue)
            {
                case BsonNull _:
                    return null;
                case BsonString bsonString: // v < 0.12.0
                    return new DocumentVersion(bsonString.AsString);
                case BsonArray bsonArray: // 0.12.0 <= v
                    return new DocumentVersion(
                        bsonArray[0].AsInt32,
                        bsonArray[1].AsInt32,
                        bsonArray[2].AsInt32,
                        bsonArray.Count >= 4 ? bsonArray[3].AsString : null);
                default: throw new NotSupportedException();
            }
        }

        private static BsonArray DocumentVersionToBsonArray(DocumentVersion documentVersion)
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

        private BsonClassMapSerializer<TModel> GetSerializer()
        {
            if (_serializer == null)
            {
                var classMap = BsonClassMap.LookupClassMap(typeof(TModel));
                _serializer = new BsonClassMapSerializer<TModel>(classMap);
            }
            return _serializer;
        }
    }
}
