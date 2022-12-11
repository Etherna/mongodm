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
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    class ReferenceModelSchema<TModel> : ModelSchemaBase, IReferenceModelSchemaBuilder<TModel>
    {
        // Constructor.
        public ReferenceModelSchema(
            ModelMap<TModel> activeMap,
            IDbContext dbContext)
            : base(activeMap, dbContext, typeof(TModel))
        { }

        // Methods.
        public IReferenceModelSchemaBuilder<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer)
        {
            AddFallbackCustomSerializerHelper(fallbackSerializer);
            return this;
        }

        public IReferenceModelSchemaBuilder<TModel> AddFallbackModelMap(
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            string? baseModelMapId = null) =>
            AddFallbackModelMap(new ModelMap<TModel>(
                "fallback",
                new BsonClassMap<TModel>(modelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId));

        public IReferenceModelSchemaBuilder<TModel> AddFallbackModelMap(ModelMap<TModel> modelMap)
        {
            AddFallbackModelMapHelper(modelMap);
            return this;
        }

        public IReferenceModelSchemaBuilder<TModel> AddSecondaryMap(
            string id,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            string? baseModelMapId = null) =>
            AddSecondaryMap(new ModelMap<TModel>(
                id,
                new BsonClassMap<TModel>(modelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId));

        public IReferenceModelSchemaBuilder<TModel> AddSecondaryMap(ModelMap<TModel> modelMap)
        {
            AddSecondaryMapHelper(modelMap);
            return this;
        }
    }
}
