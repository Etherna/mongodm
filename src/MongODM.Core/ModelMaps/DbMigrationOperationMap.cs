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

using Etherna.MongODM.Core.Models;
using Etherna.MongODM.Core.Models.DbMigrationOpAgg;
using Etherna.MongODM.Core.Serialization;

namespace Etherna.MongODM.Core.ModelMaps
{
    class DbMigrationOperationMap : IModelMapsCollector
    {
        public void Register(IDbContext dbContext)
        {
            dbContext.SchemaRegister.AddModelMapSchema<DbMigrationOperation>("afdb63c9-791b-41f8-8216-556e233df0de");

            dbContext.SchemaRegister.AddModelMapSchema<MigrationLogBase>("1696c0c9-d615-44d9-ab9b-4e3618164185");

            dbContext.SchemaRegister.AddModelMapSchema<DocumentMigrationLog>("d2b49514-464e-4b28-8b38-ad2d0cc69d3e");

            dbContext.SchemaRegister.AddModelMapSchema<IndexMigrationLog>("24d65670-a3c3-443c-977a-51112df04e2a");
        }
    }
}
