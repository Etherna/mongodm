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

using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Serialization;
using System.Linq;

namespace Etherna.MongODM.Core
{
    public class DbContextOptions
    {
        public SemanticVersion ApplicationVersion { get; set; } = "1.0.0";
        public string ConnectionString { get; set; } = "mongodb://localhost/localDb";
        public string DbName => ConnectionString.Split('?')[0]
                                                .Split('/').Last();
        public string DbOperationsCollectionName { get; set; } = "_db_ops";
        public string? Identifier { get; set; }
    }

    public class DbContextOptions<TDbContext> : DbContextOptions
        where TDbContext : class, IDbContext
    {
        public DbContextOptions()
        {
            ConnectionString = $"mongodb://localhost/{typeof(TDbContext).Name.ToLowerFirstChar()}";
        }
    }
}
