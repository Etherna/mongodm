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
using Etherna.MongODM.Core.Serialization.Serializers;
using Etherna.MongODM.Core.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public abstract class ModelMap : FreezableConfig
    {
        private readonly BsonClassMap _bsonClassMap;
        private IBsonSerializer? _serializer;

        // Constructors.
        protected ModelMap(
            string id,
            BsonClassMap bsonClassMap,
            IBsonSerializer? serializer = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            _bsonClassMap = bsonClassMap ?? throw new ArgumentNullException(nameof(bsonClassMap));
            _serializer = serializer;
        }

        // Properties.
        public string Id { get; }
        public BsonClassMap BsonClassMap
        {
            get
            {
                Freeze();
                return _bsonClassMap;
            }
        }
        public bool IsEntity => BsonClassMap.IsEntity();
        public Type ModelType => BsonClassMap.ClassType;
        public IBsonSerializer? Serializer
        {
            get
            {
                Freeze();
                return _serializer;
            }
        }

        // Methods.
        public void UseDefaultSerializer(IDbContext dbContext) =>
            ExecuteConfigAction(() =>
            {
                if (dbContext is null)
                    throw new ArgumentNullException(nameof(dbContext));
                if (_serializer != null)
                    throw new InvalidOperationException("A serializer is already setted");
                if (ModelType.IsAbstract)
                    throw new InvalidOperationException("Can't set default serializer of an abstract model");

                _serializer = GetDefaultSerializer(dbContext);
            });

        public void UseProxyGenerator(IDbContext dbContext) =>
            ExecuteConfigAction(() =>
            {
                if (dbContext is null)
                    throw new ArgumentNullException(nameof(dbContext));
                if (ModelType.IsAbstract)
                    throw new InvalidOperationException("Can't generate proxy of an abstract model");

                // Set creator.
                _bsonClassMap.SetCreator(() => dbContext.ProxyGenerator.CreateInstance(ModelType, dbContext));
            });

        // Protected methods.
        protected override void FreezeAction()
        {
            _bsonClassMap.Freeze();
        }

        protected abstract IBsonSerializer GetDefaultSerializer(IDbContext dbContext);
    }

    public class ModelMap<TModel> : ModelMap
        where TModel : class
    {
        // Constructors.
        public ModelMap(
            string id,
            BsonClassMap<TModel> bsonClassMap,
            IBsonSerializer<TModel>? serializer = null)
            : base(id, bsonClassMap, serializer)
        { }

        // Methods.
        protected override IBsonSerializer GetDefaultSerializer(IDbContext dbContext)
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));

            return new ModelMapSerializer<TModel>(
                dbContext.DbCache,
                dbContext.DocumentSemVerOptions,
                dbContext.SchemaRegister.GetModelMapsSchema<TModel>(),
                dbContext.ModelMapVersionOptions,
                dbContext.SerializerModifierAccessor);
        }
    }
}
