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

using Etherna.MongODM.Core.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class ModelMap<TModel> : ModelMapBase
        where TModel : class
    {
        // Fields.
        private readonly IDbContext dbContext;

        // Constructors.
        public ModelMap(
            string id,
            BsonClassMap<TModel> bsonClassMap,
            IDbContext dbContext,
            string? baseModelMapId = null,
            IBsonSerializer<TModel>? serializer = null)
            : base(id, baseModelMapId, bsonClassMap, serializer)
        {
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        // Protected methods.
        protected override IBsonSerializer? GetDefaultSerializer()
        {
            if (typeof(TModel).IsAbstract) //ignore to deserialize abstract types
                return null;

            return new ModelMapSerializer<TModel>(
                dbContext.DbCache,
                dbContext.DocumentSemVerOptions,
                dbContext.ModelMapVersionOptions,
                dbContext.SchemaRegister,
                dbContext.SerializerModifierAccessor);
        }
    }
}
