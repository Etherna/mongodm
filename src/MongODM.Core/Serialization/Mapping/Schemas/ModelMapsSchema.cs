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

using MongoDB.Bson.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    class ModelMapsSchema<TModel> : ModelMapsSchemaBase, IModelMapsSchemaBuilder<TModel>
        where TModel : class
    {
        // Constructor.
        public ModelMapsSchema(
            ModelMap<TModel> activeMap,
            IDbContext dbContext)
            : base(activeMap, dbContext, typeof(TModel))
        { }

        // Methods.
        public IModelMapsSchemaBuilder<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer)
        {
            AddFallbackCustomSerializerHelper(fallbackSerializer);
            return this;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The new model map instance can't be disposed")]
        public IModelMapsSchemaBuilder<TModel> AddSecondaryMap(
            string id,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            string? baseModelMapId = null,
            IBsonSerializer<TModel>? customSerializer = null)
        {
            // Verify if needs a default serializer.
            if (!typeof(TModel).IsAbstract)
                customSerializer ??= ModelMap.GetDefaultSerializer<TModel>(DbContext);

            // Create model map.
            var modelMap = new ModelMap<TModel>(
                id,
                new BsonClassMap<TModel>(modelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId,
                customSerializer);

            return AddSecondaryMap(modelMap);
        }

        public IModelMapsSchemaBuilder<TModel> AddSecondaryMap(ModelMap<TModel> modelMap)
        {
            AddSecondaryMapHelper(modelMap);
            return this;
        }
    }
}
