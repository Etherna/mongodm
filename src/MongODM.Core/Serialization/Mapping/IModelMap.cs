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
using Etherna.MongODM.Core.Serialization.Serializers;
using Etherna.MongODM.Core.Utility;
using System;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public interface IModelMap : IFreezableConfig
    {
        // Properties.
        string Id { get; }
        string? BaseModelMapId { get; }
        BsonClassMap BsonClassMap { get; }
        IBsonSerializer BsonClassMapSerializer { get; }
        bool IsEntity { get; }
        Type ModelType { get; }
        IBsonSerializer? Serializer { get; }

        // Methods.
        Task<object> FixDeserializedModelAsync(object model);
        void SetBaseModelMap(IModelMap baseModelMap);
        void UseProxyGenerator(IDbContext dbContext);
    }

    public interface IModelMap<TModel> : IModelMap
    {
        // Methods.
        Task<TModel> FixDeserializedModelAsync(TModel model);
    }
}