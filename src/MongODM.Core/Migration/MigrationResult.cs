// Copyright 2020-present Etherna SA
// This file is part of MongODM.
// 
// MongODM is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// MongODM is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with MongODM.
// If not, see <https://www.gnu.org/licenses/>.

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