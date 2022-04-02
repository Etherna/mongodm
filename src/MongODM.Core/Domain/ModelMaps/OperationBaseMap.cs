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

using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.Serialization;
using Etherna.MongODM.Core.Serialization.Serializers;

namespace Etherna.MongODM.Core.Domain.ModelMaps
{
    class OperationBaseMap : IModelMapsCollector
    {
        public void Register(IDbContext dbContext)
        {
            dbContext.SchemaRegistry.AddModelMapsSchema("ee726d4f-6e6a-44b0-bf3e-45322534c36d",
                customSerializer: new ModelMapSerializer<OperationBase>(
                    dbContext,
                    overrideDocumentSemVerOptions: new DocumentSemVerOptions
                    {
                        CurrentVersion = dbContext.LibraryVersion,
                        WriteInDocuments = false
                    }));
        }
    }
}
