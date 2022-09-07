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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Options
{
    public class DbContextOptions : IDbContextOptions
    {
        // Fields.
        private readonly List<Type> _childDbContextTypes = new();

        // Properties.
        public string ConnectionString { get; set; } = "mongodb://localhost/localDb";
        public string DbName => ConnectionString.Split('?')[0]
                                                .Split('/').Last();
        public string DbOperationsCollectionName { get; set; } = "_db_ops";
        public string? Identifier { get; set; }
        public ModelMapVersionOptions ModelMapVersion { get; set; } = new ModelMapVersionOptions();
        public IEnumerable<Type> ChildDbContextTypes => _childDbContextTypes;

        // Methods.
        public void ParentFor<TDbContext>() where
            TDbContext : class, IDbContext
        {
            if (!_childDbContextTypes.Contains(typeof(TDbContext)))
                _childDbContextTypes.Add(typeof(TDbContext));
        }
    }
}
