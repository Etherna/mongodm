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

using MongoDB.Bson.Serialization;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    public interface IModelMapsSchema : ISchema
    {
        // Properties.
        IBsonSerializer ActiveBsonClassMapSerializer { get; }
        IModelMap ActiveMap { get; }
        IReadOnlyDictionary<string, IModelMap> AllMapsDictionary { get; }
        IDbContext DbContext { get; }
        IBsonSerializer? FallbackSerializer { get; }
        IEnumerable<IModelMap> SecondaryMaps { get; }
    }
}