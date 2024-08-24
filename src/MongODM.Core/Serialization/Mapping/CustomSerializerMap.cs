﻿// Copyright 2020-present Etherna SA
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Etherna.MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    internal sealed class CustomSerializerMap<TModel> : MapBase, ICustomSerializerMapBuilder<TModel>
    {
        // Constructor.
        public CustomSerializerMap(
            IBsonSerializer<TModel> customSerializer)
            : base(typeof(TModel))
        {
            ActiveSerializer = customSerializer;
        }

        // Properties.
        public override IBsonSerializer ActiveSerializer { get; }
        public override Type? ProxyModelType => default;
    }
}
