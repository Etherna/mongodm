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

using Etherna.MongODM.Core.Models;
using Etherna.MongODM.Core.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Schemas
{
    public abstract class ModelMapSchema
    {
        // Constructors.
        public ModelMapSchema(
            string id,
            BsonClassMap modelMap,
            IBsonSerializer? serializer = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            ModelMap = modelMap ?? throw new ArgumentNullException(nameof(modelMap));
            Serializer = serializer;
        }

        // Properties.
        public string Id { get; }
        public BsonClassMap ModelMap { get; }
        public abstract Type ModelType { get; }
        public IBsonSerializer? Serializer { get; private set; }

        // Methods.
        public void UseDefaultSerializer(IDbContext dbContext)
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));
            if (Serializer != null)
                throw new InvalidOperationException("A serializer is already setted");
            if (ModelType.IsAbstract)
                throw new InvalidOperationException("Can't set default serializer of an abstract model");

            Serializer = GetDefaultSerializer(dbContext);
        }

        public void UseProxyGenerator(IDbContext dbContext)
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));
            if (ModelType.IsAbstract)
                throw new InvalidOperationException("Can't generate proxy of an abstract model");

            // Set creator.
            ModelMap.SetCreator(() => dbContext.ProxyGenerator.CreateInstance(ModelType, dbContext));
        }

        // Protected methods.
        protected abstract IBsonSerializer GetDefaultSerializer(IDbContext dbContext);
    }

    public class ModelMapSchema<TModel> : ModelMapSchema
        where TModel : class
    {
        // Constructors.
        public ModelMapSchema(
            string id,
            BsonClassMap<TModel> modelMap,
            IBsonSerializer<TModel>? serializer = null)
            : base(id, modelMap, serializer)
        { }

        // Properties.
        public override Type ModelType => typeof(TModel);

        // Methods.
        protected override IBsonSerializer GetDefaultSerializer(IDbContext dbContext)
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));

            return new ExtendedClassMapSerializer<TModel>(
                dbContext.DbCache,
                dbContext.ApplicationVersion,
                dbContext.SerializerModifierAccessor,
                dbContext.SchemaRegister)
            { AddVersion = typeof(IEntityModel).IsAssignableFrom(ModelType) }; //true only for entity models
        }
    }
}
