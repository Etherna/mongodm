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

namespace Etherna.MongODM.Core.Migration
{
    public class MigrationResult
    {
        // Constructors.
        private MigrationResult() { }

        // Properties.
        public Exception? Exception { get; private set; }
        public long MigratedDocuments { get; private set; }
        public bool Succeded { get; private set; }

        // Methods.
        public static MigrationResult Failed(long migratedDocuments, Exception? e = null) =>
            new()
            {
                Exception = e,
                MigratedDocuments = migratedDocuments,
                Succeded = false
            };

        public static MigrationResult Succeeded(long migratedDocuments) =>
            new()
            {
                MigratedDocuments = migratedDocuments,
                Succeded = true
            };
    }
}