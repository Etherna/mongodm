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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public abstract class ModelMap : FreezableConfig, IModelMap
    {
        // Fields.
        private IBsonSerializer _bsonClassMapSerializer = default!;

        // Constructors.
        protected ModelMap(
            string id,
            string? baseModelMapId,
            BsonClassMap bsonClassMap,
            IBsonSerializer? serializer)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException($"'{nameof(id)}' cannot be null or empty", nameof(id));

            Id = id;
            BaseModelMapId = baseModelMapId;
            BsonClassMap = bsonClassMap ?? throw new ArgumentNullException(nameof(bsonClassMap));
            Serializer = serializer;
        }

        // Properties.
        public string Id { get; }
        public string? BaseModelMapId { get; private set; }
        public BsonClassMap BsonClassMap { get; }
        public IBsonSerializer BsonClassMapSerializer
        {
            get
            {
                if (_bsonClassMapSerializer is null)
                {
                    var classMapSerializerDefinition = typeof(BsonClassMapSerializer<>);
                    var classMapSerializerType = classMapSerializerDefinition.MakeGenericType(ModelType);
                    _bsonClassMapSerializer = (IBsonSerializer)Activator.CreateInstance(classMapSerializerType, BsonClassMap);
                }
                return _bsonClassMapSerializer;
            }
        }
        public bool IsEntity => BsonClassMap.IsEntity();
        public Type ModelType => BsonClassMap.ClassType;
        public IBsonSerializer? Serializer { get; }

        // Methods.
        public Task<object> FixDeserializedModelAsync(object model) =>
            FixDeserializedModelHelperAsync(model);

        public void SetBaseModelMap(IModelMap baseModelMap) =>
            ExecuteConfigAction(() =>
            {
                if (baseModelMap is null)
                    throw new ArgumentNullException(nameof(baseModelMap));

                BaseModelMapId = baseModelMap.Id;
                BsonClassMap.SetBaseClassMap(baseModelMap.BsonClassMap);
            });

        public void UseProxyGenerator(IDbContext dbContext) =>
            ExecuteConfigAction(() =>
            {
                if (dbContext is null)
                    throw new ArgumentNullException(nameof(dbContext));
                if (ModelType.IsAbstract)
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
                BsonClassMap.SetCreator(() => dbContext.ProxyGenerator.CreateInstance(ModelType, dbContext));
            });

        // Protected methods.
        protected abstract Task<object> FixDeserializedModelHelperAsync(object model);

        protected override void FreezeAction()
        {
            // Freeze bson class maps.
            BsonClassMap.Freeze();
        }

        // Static methods.
        public static IBsonSerializer<TModel> GetDefaultSerializer<TModel>(IDbContext dbContext)
            where TModel : class
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));

            return new ModelMapSerializer<TModel>(dbContext);
        }
    }

    public class ModelMap<TModel> : ModelMap, IModelMap<TModel>
    {
        private readonly Func<TModel, Task<TModel>>? fixDeserializedModelFunc;

        // Constructors.
        public ModelMap(
            string id,
            BsonClassMap<TModel>? bsonClassMap = null,
            string? baseModelMapId = null,
            Func<TModel, Task<TModel>>? fixDeserializedModelFunc = null,
            IBsonSerializer<TModel>? serializer = null)
            : base(id, baseModelMapId, bsonClassMap ?? new BsonClassMap<TModel>(), serializer)
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
}
