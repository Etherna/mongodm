﻿//   Copyright 2020-present Etherna Sagl
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
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    internal class ModelSchema<TModel> : ModelSchemaBase, IModelSchemaBuilder<TModel>
        where TModel : class
    {
        // Constructor.
        public ModelSchema(IDbContext dbContext)
            : base(dbContext, typeof(TModel))
        { }

        // Methods.
        public IModelSchemaBuilder<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer)
        {
            AddFallbackCustomSerializerHelper(fallbackSerializer);
            return this;
        }

        public IModelSchemaBuilder<TModel> AddFallbackModelMap(
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            string? baseModelMapId = null,
            IBsonSerializer<TModel>? customSerializer = null) =>
            AddFallbackModelMap(new ModelMap<TModel>(
                "fallback",
                new BsonClassMap<TModel>(modelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId,
                null,
                customSerializer,
                this));

        public IModelSchemaBuilder<TModel> AddFallbackModelMap(IModelMap<TModel> modelMap)
        {
            AddFallbackModelMapHelper(modelMap);
            return this;
        }

        public IModelSchemaBuilder<TModel> AddSecondaryMap(IModelMap<TModel> modelMap)
        {
            AddSecondaryMapHelper(modelMap);
            return this;
        }
    }
}
