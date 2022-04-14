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
using Etherna.MongODM.Core.ProxyModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Etherna.MongODM.Core.Serialization.Serializers
{
    public class ReferenceSerializer<TModelBase, TKey> :
        SerializerBase<TModelBase>,
        IBsonSerializer<TModelBase>,
        IBsonDocumentSerializer,
        IBsonIdProvider,
        IReferenceContainerSerializer,
        IDisposable
        where TModelBase : class, IEntityModel<TKey>
    {
        // Fields.
        private IDiscriminatorConvention _discriminatorConvention = default!;

        private readonly ReaderWriterLockSlim configLockAdapters = new(LockRecursionPolicy.SupportsRecursion);
        private readonly IDbContext dbContext;
        private bool disposed;
        private readonly ReferenceSerializerConfiguration configuration;
        private readonly IDictionary<Type, IBsonSerializer> registeredAdapters = new Dictionary<Type, IBsonSerializer>();

        // Constructor.
        public ReferenceSerializer(
            IDbContext dbContext,
            Action<ReferenceSerializerConfiguration> configure)
        {
            if (configure is null)
                throw new ArgumentNullException(nameof(configure));

            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            configuration = new ReferenceSerializerConfiguration(dbContext);
            configure(configuration);
            configuration.Freeze();
        }

        // Dispose.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            // Dispose managed resources.
            if (disposing)
            {
                configLockAdapters.Dispose();
                configuration.Dispose();
            }

            disposed = true;
        }

        // Properties.
        public IEnumerable<BsonClassMap> AllChildClassMaps => configuration.Schemas.Values
            .SelectMany(schema => schema.AllMapsDictionary.Values
                .Select(map => map.BsonClassMap));

        public IDiscriminatorConvention DiscriminatorConvention
        {
            get
            {
                if (_discriminatorConvention == null)
                    _discriminatorConvention = dbContext.DiscriminatorRegistry.LookupDiscriminatorConvention(typeof(TModelBase));
                return _discriminatorConvention;
            }
        }

        public bool UseCascadeDelete => configuration.UseCascadeDelete;

        // Methods.
        public override TModelBase Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            // Check bson type.
            var bsonType = context.Reader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Document:
                    break;
                case BsonType.Null:
                    context.Reader.ReadNull();
                    return null!;
                default:
                    var message = $"Expected a nested document representing the serialized form of a {nameof(TModelBase)} value, but found a value of type {bsonType} instead.";
                    throw new InvalidOperationException(message);
            }

            // Find pre-deserialization informations.
            //get actual type
            var actualType = DiscriminatorConvention.GetActualType(context.Reader, args.NominalType);

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
            //get serializer
            var serializer = configuration.GetSerializer(actualType, modelMapId);
            if (serializer is null)
                throw new InvalidOperationException($"Can't identify a valid serializer for type {actualType.Name}");

            var model = serializer.Deserialize(localContext, args) as TModelBase;

            // Process model (if proxy).
            /* Proxy models enable different features. Anyway, if the model as not been created as a proxy
             * (for example for tests scope) these additional operations are not possible or required.
             * In this case, simply return the model as is.
             */
            if (model != null &&
                dbContext.ProxyGenerator.IsProxyType(model.GetType()))
            {
                var id = model.Id;
                if (id == null) //ignore refered instances without id
                    return null!;

                // Check if model as been loaded in cache.
                if (dbContext.DbCache.LoadedModels.ContainsKey(id) &&
                    !dbContext.SerializerModifierAccessor.IsNoCacheEnabled)
                {
                    var cachedModel = (TModelBase)dbContext.DbCache.LoadedModels[id];

                    if (((IReferenceable)cachedModel).IsSummary)
                    {
                        // Execute merging between summary models.
                        var sourceMembers = ((IReferenceable)model).SettedMemberNames
                            .Except(((IReferenceable)cachedModel).SettedMemberNames)
                            .Select(memberName => cachedModel.GetType().GetMember(memberName).Single())
                            .ToArray();

                        //temporary disable auditing
                        ((IAuditable)cachedModel).DisableAuditing();

                        foreach (var member in sourceMembers)
                        {
                            var value = ReflectionHelper.GetValue(model, member);
                            ReflectionHelper.SetValue(cachedModel, member, value);
                        }

                        //reenable auditing
                        ((IAuditable)cachedModel).EnableAuditing();

                        ((IReferenceable)cachedModel).SetAsSummary(sourceMembers.Select(m => m.Name));
                    }

                    // Return the cached model.
                    model = cachedModel;
                }
                else
                {
                    // Set model as summarizable.
                    if (dbContext.SerializerModifierAccessor.IsReadOnlyReferencedIdEnabled)
                    {
                        ((IReferenceable)model).ClearSettedMembers();
                        ((IReferenceable)model).SetAsSummary(new[] { nameof(model.Id) });
                    }
                    else
                    {
                        ((IReferenceable)model).SetAsSummary(((IReferenceable)model).SettedMemberNames);
                    }

                    // Enable auditing.
                    ((IAuditable)model).EnableAuditing();

                    // Add in cache.
                    if (!dbContext.SerializerModifierAccessor.IsNoCacheEnabled)
                        dbContext.DbCache.AddModel(model.Id!, model);
                }
            }

            return model!;
        }

        public IBsonSerializer<TModel> GetAdapter<TModel>()
            where TModel : class, TModelBase
        {
            configLockAdapters.EnterReadLock();
            try
            {
                if (registeredAdapters.ContainsKey(typeof(TModel)))
                {
                    return (IBsonSerializer<TModel>)registeredAdapters[typeof(TModel)];
                }
            }
            finally
            {
                configLockAdapters.ExitReadLock();
            }

            configLockAdapters.EnterWriteLock();
            try
            {
                if (!registeredAdapters.ContainsKey(typeof(TModel)))
                {
                    registeredAdapters.Add(typeof(TModel), new ReferenceSerializerAdapter<TModelBase, TModel, TKey>(this));
                }
                return (IBsonSerializer<TModel>)registeredAdapters[typeof(TModel)];
            }
            finally
            {
                configLockAdapters.ExitWriteLock();
            }
        }

        public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var serializer = configuration.Schemas[document.GetType()].ActiveBsonClassMapSerializer;

            if (serializer is IBsonIdProvider idProvider)
                return idProvider.GetDocumentId(document, out id, out idNominalType, out idGenerator);

            id = default!;
            idNominalType = default!;
            idGenerator = default!;
            return false;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TModelBase value)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            // Check value type.
            if (value == null)
            {
                context.Writer.WriteNull();
                return;
            }

            // Clear extra elements. They are never needed with references.
            value.ExtraElements?.Clear();

            // Initialize localContext, bsonDocument and bsonWriter.
            var bsonDocument = new BsonDocument();
            using var bsonWriter = new BsonDocumentWriter(bsonDocument);
            var localContext = BsonSerializationContext.CreateRoot(
                bsonWriter,
                builder => builder.IsDynamicType = context.IsDynamicType);

            // Serialize.
            var serializer = configuration.Schemas[value.GetType()].ActiveBsonClassMapSerializer;
            serializer.Serialize(localContext, args, value);

            // Add additional data.
            //add model map id
            if (bsonDocument.Contains(dbContext.Options.ModelMapVersion.ElementName))
                bsonDocument.Remove(dbContext.Options.ModelMapVersion.ElementName);
            var modelMapIdElement = configuration.GetActiveModelMapIdBsonElement(
                dbContext.ProxyGenerator.PurgeProxyType(value.GetType()));
            bsonDocument.InsertAt(0, modelMapIdElement);

            // Serialize document.
            BsonDocumentSerializer.Instance.Serialize(context, args, bsonDocument);
        }

        public void SetDocumentId(object document, object id)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var serializer = configuration.Schemas[document.GetType()].ActiveBsonClassMapSerializer;

            if (serializer is IBsonIdProvider idProvider)
                idProvider.SetDocumentId(document, id);
            else
                throw new InvalidOperationException("Can't find a valid serializer");
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            var modelType = configuration.Schemas.Values
                .Select(s => s.ActiveMap.BsonClassMap)
                .Where(cm => cm.GetMemberMap(memberName) != null)
                .First()
                .ClassType;
            var serializer = configuration.Schemas[modelType].ActiveBsonClassMapSerializer;

            if (serializer is IBsonDocumentSerializer documentSerializer)
                return documentSerializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);
            else
                throw new InvalidOperationException("Can't find a valid serializer");
        }

        // Helpers.
        private static string? BsonValueToModelMapId(BsonValue bsonValue) =>
            bsonValue switch
            {
                BsonNull _ => null,
                BsonString bsonString => bsonString.AsString,
                _ => throw new NotSupportedException(),
            };
    }
}
