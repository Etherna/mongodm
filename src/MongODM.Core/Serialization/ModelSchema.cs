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

namespace Etherna.MongODM.Core.Serialization
{
    public abstract class ModelSchema
    {
        // Constructors.
        public ModelSchema(
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
        public BsonClassMap? ProxyModelMap { get; set; }
        public IBsonSerializer? Serializer { get; set; }
    }

    public class ModelSchema<TModel> : ModelSchema
    {
        // Constructors.
        public ModelSchema(
            string id,
            BsonClassMap<TModel> modelMap,
            IBsonSerializer<TModel>? serializer = null)
            : base(id, modelMap, serializer)
        { }

        // Properties.
        public override Type ModelType => typeof(TModel);
    }
}
