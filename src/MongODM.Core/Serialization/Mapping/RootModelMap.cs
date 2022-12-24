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
    internal class RootModelMap<TModel> : ModelMapBase, IRootModelMapBuilder<TModel>
        where TModel : class
    {
        // Constructor.
        public RootModelMap(IDbContext dbContext)
            : base(dbContext, typeof(TModel))
        { }

        // Methods.
        public IRootModelMapBuilder<TModel> AddFallbackCustomSerializerMap(IBsonSerializer<TModel> fallbackSerializer)
        {
            AddFallbackCustomSerializerHelper(fallbackSerializer);
            return this;
        }

        public IRootModelMapBuilder<TModel> AddFallbackModelMapSchema(
            Action<BsonClassMap<TModel>>? modelMapSchemaInitializer = null,
            string? baseModelMapSchemaId = null,
            IBsonSerializer<TModel>? customSerializer = null) =>
            AddFallbackModelMapSchema(new ModelMapSchema<TModel>(
                "fallback",
                new BsonClassMap<TModel>(modelMapSchemaInitializer ?? (cm => cm.AutoMap())),
                baseModelMapSchemaId,
                null,
                customSerializer,
                this));

        public IRootModelMapBuilder<TModel> AddFallbackModelMapSchema(IModelMapSchema<TModel> modelMapSchema)
        {
            AddFallbackModelMapSchemaHelper(modelMapSchema);
            return this;
        }

        public IRootModelMapBuilder<TModel> AddSecondaryModelMapSchema(IModelMapSchema<TModel> modelMapSchema)
        {
            AddSecondaryModelMapSchemaHelper(modelMapSchema);
            return this;
        }
    }
}
