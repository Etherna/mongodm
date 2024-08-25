﻿// Copyright 2020-present Etherna SA
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
