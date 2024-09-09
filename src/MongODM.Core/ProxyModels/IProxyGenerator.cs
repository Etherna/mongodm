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

namespace Etherna.MongODM.Core.ProxyModels
{
    public interface IProxyGenerator
    {
        // Properties.
        bool DisableCreationWithProxyTypes { get; set; }

        // Methods.
        object CreateInstance(Type type, IDbContext dbContext, params object[] constructorArguments);
        TModel CreateInstance<TModel>(IDbContext dbContext, params object[] constructorArguments);
        bool IsProxyType(Type type);
        Type PurgeProxyType(Type type);
    }
}