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
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Serialization.Serializers.Config;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private readonly ReaderWriterLockSlim configLockAdapters = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly IDbContext dbContext;
        private readonly ReferenceSerializerConfiguration configuration;
        private readonly IDictionary<Type, IBsonSerializer> registeredAdapters = new Dictionary<Type, IBsonSerializer>();

        // Constructor and dispose.
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

        public void Dispose()
        {
            configLockAdapters.Dispose();
            configuration.Dispose();
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
                    _discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(TModelBase));
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
            var bsonReader = context.Reader;
            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Document:
                    break;
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null!;
                default:
                    var message = $"Expected a nested document representing the serialized form of a {nameof(TModelBase)} value, but found a value of type {bsonType} instead.";
                    throw new InvalidOperationException(message);
            }

            // Find pre-deserialization informations.
            //get actual type
            var actualType = DiscriminatorConvention.GetActualType(bsonReader, args.NominalType);

            //get model map id
            var modelMapId = bsonReader.IdempotentFindStringElement(dbContext.ModelMapVersionOptions.ElementName);

            // Deserialize object.
            var serializer = configuration.GetSerializer(actualType, modelMapId);
            var model = serializer.Deserialize(context, args) as TModelBase;

            // Process model.
            if (model != null)
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

            var modelType = dbContext.ProxyGenerator.PurgeProxyType(document.GetType());
            var serializer = configuration.GetActiveModelMapSerializer(modelType);

            if (serializer is IBsonIdProvider idProvider)
                return idProvider.GetDocumentId(document, out id, out idNominalType, out idGenerator);

            id = default!;
            idNominalType = default!;
            idGenerator = default!;
            return false;
        }

        public ReferenceSerializer<TModelBase, TKey> RegisterProxyType<TModel>()
        {
            var proxyType = dbContext.ProxyGenerator.CreateInstance<TModel>(dbContext)!.GetType();

            // Initialize class map.
            var createBsonClassMapInfo = GetType().GetMethod(nameof(CreateBsonClassMap), BindingFlags.Instance | BindingFlags.NonPublic);
            var createBsonClassMap = createBsonClassMapInfo.MakeGenericMethod(proxyType);

            var classMap = (BsonClassMap)createBsonClassMap.Invoke(this, new object[] { null! });

            // Add info to dictionary of registered types.
            configLockClassMaps.EnterWriteLock();
            try
            {
                registeredClassMaps.Add(proxyType, classMap);
            }
            finally
            {
                configLockClassMaps.ExitWriteLock();
            }

            // Return this for cascade use.
            return this;
        }

        public ReferenceSerializer<TModelBase, TKey> RegisterType<TModel>(Action<BsonClassMap<TModel>>? classMapInitializer = null)
            where TModel : class
        {
            // Initialize class map.
            var classMap = CreateBsonClassMap(classMapInitializer ?? (cm => cm.AutoMap()));

            // Set creator.
            if (!typeof(TModel).IsAbstract)
                classMap.SetCreator(() => dbContext.ProxyGenerator.CreateInstance<TModel>(dbContext));

            // Add info to dictionary of registered types.
            configLockClassMaps.EnterWriteLock();
            try
            {
                registeredClassMaps.Add(typeof(TModel), classMap);
            }
            finally
            {
                configLockClassMaps.ExitWriteLock();
            }

            // Return this for cascade use.
            return this;
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

            // Clear extra elements.
            value.ExtraElements?.Clear();

            // Serialize object.
            var serializer = GetSerializer(value.GetType());
            serializer.Serialize(context, args, value);
        }

        public void SetDocumentId(object document, object id)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var documentType = dbContext.ProxyGenerator.PurgeProxyType(document.GetType());
            var serializer = (IBsonIdProvider)GetSerializer(documentType);
            serializer.SetDocumentId(document, id);
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            // Identify class map and get information
            configLockClassMaps.EnterReadLock();
            try
            {
                var modelType = (from pair in registeredClassMaps
                                 where pair.Value.GetMemberMap(memberName) != null
                                 select pair.Key).FirstOrDefault();
                var serializer = (IBsonDocumentSerializer)GetSerializer(modelType);
                return serializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);
            }
            finally
            {
                configLockClassMaps.ExitReadLock();
            }
        }

        // Helpers.
        /// <summary>
        /// Create a new BsonClassMap for type TModel, and link its baseClassMap if already registered
        /// </summary>
        /// <typeparam name="TModel">The destination model type of class map</typeparam>
        /// <param name="classMapInitializer">The class map inizializer. Empty initilization if null</param>
        /// <returns>The new created class map</returns>
        private BsonClassMap<TModel> CreateBsonClassMap<TModel>(Action<BsonClassMap<TModel>>? classMapInitializer = null)
        {
            classMapInitializer ??= cm => { };

            BsonClassMap<TModel> classMap = new BsonClassMap<TModel>(classMapInitializer);
            var baseType = typeof(TModel).BaseType;
            configLockClassMaps.EnterReadLock();
            try
            {
                if (registeredClassMaps.ContainsKey(baseType))
                {
                    // Inject base class map.
                    typeof(BsonClassMap).GetField("_baseClassMap", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(classMap, registeredClassMaps[baseType]);
                }
            }
            finally
            {
                configLockClassMaps.ExitReadLock();
            }

            classMap.Freeze();
            return classMap;
        }
    }
}
