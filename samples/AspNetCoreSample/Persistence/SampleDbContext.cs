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

using Etherna.MongODM.AspNetCoreSample.Models;
using Etherna.MongODM.AspNetCoreSample.Models.ModelMaps;
using Etherna.MongODM.Core;
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etherna.MongODM.AspNetCoreSample.Persistence
{
    public class SampleDbContext : DbContext, ISampleDbContext
    {
        public IRepository<Cat, string> Cats { get; } = new Repository<Cat, string>("cats");

        protected override IEnumerable<IModelMapsCollector> ModelMapsCollectors =>
            new IModelMapsCollector[]
            {
                new ModelBaseMap(),
                new CatMap()
            };

        protected override Task SeedAsync()
        {
            // Seed here.

            return base.SeedAsync();
        }
    }
}
