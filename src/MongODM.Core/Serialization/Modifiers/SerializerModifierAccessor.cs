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

using Etherna.ExecContext;
using System;

namespace Etherna.MongODM.Core.Serialization.Modifiers
{
    public class SerializerModifierAccessor : ISerializerModifierAccessor
    {
        // Fields.
        private readonly IExecutionContext executionContext;

        // Constructors.
        public SerializerModifierAccessor(
            IExecutionContext executionContext)
        {
            this.executionContext = executionContext;
        }

        // Properties.
        public bool IsReadOnlyReferencedIdEnabled =>
            ReferenceSerializerModifier.IsReadOnlyIdEnabled(executionContext);

        public bool IsNoCacheEnabled => 
            CacheSerializerModifier.IsNoCacheEnabled(executionContext);

        // Methods.
        public IDisposable EnableCacheSerializerModifier(bool noCache) =>
            new CacheSerializerModifier(executionContext)
            {
                NoCache = noCache
            };

        public IDisposable EnableReferenceSerializerModifier(bool readOnlyId) =>
            new ReferenceSerializerModifier(executionContext)
            {
                ReadOnlyId = readOnlyId
            };
    }
}
