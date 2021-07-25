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

namespace Etherna.MongODM.Core.Options
{
    public class DbContextOptions : IDbContextOptions
    {
        // Fields.
        private string _connectionString = default!;

        // Properties.
        public string ConnectionString
        {
            get
            {
                if (_connectionString is null)
                    _connectionString = $"mongodb://localhost/{DbName}";
                return _connectionString;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(ConnectionString));
                _connectionString = value;
            }
        }
        public string DbName { get; set; } = "localDb";
        public string DbOperationsCollectionName { get; set; } = "_db_ops";
        public bool DisableAutomaticSeed { get; set; }
        public DocumentSemVerOptions DocumentSemVer { get; set; } = new DocumentSemVerOptions();
        public string? Identifier { get; set; }
        public ModelMapVersionOptions ModelMapVersion { get; set; } = new ModelMapVersionOptions();
    }
}
