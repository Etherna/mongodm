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
            BsonClassMap classMap,
            IBsonSerializer? customSerializer)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            ClassMap = classMap ?? throw new ArgumentNullException(nameof(classMap));
            CustomSerializer = customSerializer;
        }

        // Properties.
        public string Id { get; }
        public BsonClassMap ClassMap { get; }
        public IBsonSerializer? CustomSerializer { get; }
        public abstract Type ModelType { get; }
    }

    public class ModelSchema<TModel> : ModelSchema
    {
        // Constructors.
        /// <param name="id">The schema Id</param>
        /// <param name="classMap">The class map</param>
        /// <param name="customSerializer">Custom serializer</param>
        public ModelSchema(
            string id,
            BsonClassMap<TModel> classMap,
            IBsonSerializer<TModel>? customSerializer)
            : base(id, classMap, customSerializer)
        { }

        // Properties.
        public override Type ModelType => typeof(TModel);
    }
}
