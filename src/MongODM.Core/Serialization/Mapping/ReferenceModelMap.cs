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

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    internal class ReferenceModelMap<TModel> : ModelMapBase, IReferenceModelMapBuilder<TModel>
    {
        // Constructor.
        public ReferenceModelMap(
            IDbContext dbContext)
            : base(dbContext, typeof(TModel))
        { }

        // Methods.
        public IReferenceModelMapBuilder<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer)
        {
            AddFallbackCustomSerializerHelper(fallbackSerializer);
            return this;
        }

        public IReferenceModelMapBuilder<TModel> AddFallbackModelMapSchema(
            Action<BsonClassMap<TModel>>? modelMapSchemaInitializer = null,
            string? baseModelMapSchemaId = null) =>
            AddFallbackModelMapSchema(new ModelMapSchema<TModel>(
                "fallback",
                new BsonClassMap<TModel>(modelMapSchemaInitializer ?? (cm => cm.AutoMap())),
                baseModelMapSchemaId,
                null,
                null,
                this));

        public IReferenceModelMapBuilder<TModel> AddFallbackModelMapSchema(ModelMapSchema<TModel> modelMapSchema)
        {
            AddFallbackModelMapSchemaHelper(modelMapSchema);
            return this;
        }

        public IReferenceModelMapBuilder<TModel> AddSecondaryModelMapSchema(
            string id,
            Action<BsonClassMap<TModel>>? modelMapSchemaInitializer = null,
            string? baseModelMapSchemaId = null) =>
            AddSecondaryModelMapSchema(new ModelMapSchema<TModel>(
                id,
                new BsonClassMap<TModel>(modelMapSchemaInitializer ?? (cm => cm.AutoMap())),
                baseModelMapSchemaId,
                null,
                null,
                this));

        public IReferenceModelMapBuilder<TModel> AddSecondaryModelMapSchema(ModelMapSchema<TModel> modelMapSchema)
        {
            AddSecondaryModelMapSchemaHelper(modelMapSchema);
            return this;
        }
    }
}
