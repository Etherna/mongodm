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

using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Serialization.Serializers;
using Etherna.MongODM.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public abstract class ModelMapSchema : FreezableConfig, IModelMapSchema
    {
        // Fields.
        private readonly List<IMemberMap> _generatedMemberMaps = new();
        private IBsonSerializer? _serializer;

        private readonly IBsonSerializer? customSerializer;

        // Constructors.
        internal protected ModelMapSchema(
            string id,
            string? baseModelMapSchemaId,
            BsonClassMap bsonClassMap,
            IBsonSerializer? customSerializer,
            IModelMap modelMap)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException($"'{nameof(id)}' cannot be null or empty", nameof(id));
            if (bsonClassMap.ClassType != modelMap.ModelType)
                throw new ArgumentException($"'{nameof(bsonClassMap)}'.ClassType must be {modelMap.ModelType.Name}, instead it is {bsonClassMap.ClassType.Name}");

            Id = id;
            BaseModelMapSchemaId = baseModelMapSchemaId;
            BsonClassMap = bsonClassMap ?? throw new ArgumentNullException(nameof(bsonClassMap));
            this.customSerializer = customSerializer;
            ModelMap = modelMap ?? throw new ArgumentNullException(nameof(modelMap));
        }

        // Properties.
        public string Id { get; }
        public string? BaseModelMapSchemaId { get; private set; }
        public BsonClassMap BsonClassMap { get; }
        public IEnumerable<IMemberMap> GeneratedMemberMaps => _generatedMemberMaps;
        public IMemberMap? IdMemberMap => GeneratedMemberMaps.FirstOrDefault(mm => mm.IsIdMember);
        public bool IsCurrentActive => ModelMap.ActiveSchema == this;
        public bool IsEntity => BsonClassMap.IsEntity();
        public IModelMap ModelMap { get; }
        public IBsonSerializer Serializer => _serializer ??= customSerializer ?? GetDefaultSerializer();

        // Methods.
        public Task<object> FixDeserializedModelAsync(object model) =>
            FixDeserializedModelHelperAsync(model);

        public void SetBaseModelMapSchema(IModelMapSchema baseModelMapSchema) =>
            ExecuteConfigAction(() =>
            {
                if (baseModelMapSchema is null)
                    throw new ArgumentNullException(nameof(baseModelMapSchema));

                BaseModelMapSchemaId = baseModelMapSchema.Id;
                BsonClassMap.SetBaseClassMap(baseModelMapSchema.BsonClassMap);
            });

        public void UseProxyGenerator(IDbContext dbContext) =>
            ExecuteConfigAction(() =>
            {
                if (dbContext is null)
                    throw new ArgumentNullException(nameof(dbContext));
                if (ModelMap.ModelType.IsAbstract)
                    throw new InvalidOperationException("Can't generate proxy of an abstract model");

                // Remove CreatorMaps.
                while (BsonClassMap.CreatorMaps.Any())
                {
                    var memberInfo = BsonClassMap.CreatorMaps.First().MemberInfo;
                    switch (memberInfo)
                    {
                        case ConstructorInfo constructorInfo:
                            BsonClassMap.UnmapConstructor(constructorInfo);
                            break;
                        case MethodInfo methodInfo:
                            BsonClassMap.UnmapFactoryMethod(methodInfo);
                            break;
                        default: throw new InvalidOperationException();
                    }
                }

                // Set creator.
                BsonClassMap.SetCreator(() => dbContext.ProxyGenerator.CreateInstance(ModelMap.ModelType, dbContext));
            });

        public bool TryUseProxyGenerator(IDbContext dbContext)
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));

            // Verify if can use proxy model.
            if (ModelMap.ModelType != typeof(object) &&
                !ModelMap.ModelType.IsAbstract &&
                !dbContext.ProxyGenerator.IsProxyType(ModelMap.ModelType))
            {
                UseProxyGenerator(dbContext);
                return true;
            }

            return false;
        }

        // Protected methods.
        protected abstract Task<object> FixDeserializedModelHelperAsync(object model);

        protected override void FreezeAction()
        {
            // Freeze bson class maps.
            BsonClassMap.Freeze();
        }

        // Internal methods.
        internal void AddGeneratedMemberMap(IMemberMap memberMap) => _generatedMemberMaps.Add(memberMap);

        // Helpers.
        private IBsonSerializer GetDefaultSerializer()
        {
            var modelMapSerializerDefinition = typeof(ModelMapSerializer<>);
            var modelMapSerializerType = modelMapSerializerDefinition.MakeGenericType(ModelMap.ModelType);
            return (IBsonSerializer)Activator.CreateInstance(modelMapSerializerType, ModelMap.DbContext);
        }
    }

    public class ModelMapSchema<TModel> : ModelMapSchema, IModelMapSchema<TModel>
    {
        private readonly Func<TModel, Task<TModel>>? fixDeserializedModelFunc;

        // Constructors.
        internal ModelMapSchema(
            string id,
            BsonClassMap<TModel>? bsonClassMap,
            string? baseModelMapSchemaId,
            Func<TModel, Task<TModel>>? fixDeserializedModelFunc,
            IBsonSerializer? customSerializer,
            IModelMap modelMap)
            : base(id, baseModelMapSchemaId, bsonClassMap ?? new BsonClassMap<TModel>(cm => cm.AutoMap()), customSerializer, modelMap)
        {
            this.fixDeserializedModelFunc = fixDeserializedModelFunc;
        }

        // Methods.
        public async Task<TModel> FixDeserializedModelAsync(TModel model)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            return (TModel)await FixDeserializedModelHelperAsync(model).ConfigureAwait(false);
        }

        // Protected methods.
        protected override async Task<object> FixDeserializedModelHelperAsync(
            object model)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            return fixDeserializedModelFunc is not null ?
                (await fixDeserializedModelFunc((TModel)model).ConfigureAwait(false))! :
                model;
        }
    }

    public class ModelMapSchema<TModel, TOverrideNominal> : ModelMapSchema, IModelMapSchema<TModel>
        where TOverrideNominal : class, TModel
    {
        private readonly Func<TOverrideNominal, Task<TOverrideNominal>>? fixDeserializedModelFunc;

        // Constructors.
        internal ModelMapSchema(
            string id,
            BsonClassMap<TOverrideNominal>? bsonClassMap,
            string? baseModelMapSchemaId,
            Func<TOverrideNominal, Task<TOverrideNominal>>? fixDeserializedModelFunc,
            IBsonSerializer<TOverrideNominal>? customSerializer,
            IModelMap modelMap)
            : base(id, baseModelMapSchemaId, bsonClassMap ?? new BsonClassMap<TOverrideNominal>(cm => cm.AutoMap()), customSerializer, modelMap)
        {
            this.fixDeserializedModelFunc = fixDeserializedModelFunc;
        }

        // Methods.
        public async Task<TModel> FixDeserializedModelAsync(TModel model)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            return (TModel)await FixDeserializedModelHelperAsync(model).ConfigureAwait(false);
        }

        // Protected methods.
        protected override async Task<object> FixDeserializedModelHelperAsync(
            object model)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            return fixDeserializedModelFunc is not null ?
                (await fixDeserializedModelFunc((TOverrideNominal)model).ConfigureAwait(false))! :
                model;
        }
    }
}
