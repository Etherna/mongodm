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
using Etherna.ExecContext.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Modifiers
{
    internal sealed class CacheSerializerModifier : IDisposable
    {
        // Consts.
        private const string ModifierKey = "CacheSerializerModifier";

        // Fields.
        private readonly ICollection<CacheSerializerModifier> requestes;

        // Constructors and dispose.
        public CacheSerializerModifier(IExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            if (context.Items is null)
                throw new ExecutionContextNotFoundException();

            if (!context.Items.ContainsKey(ModifierKey))
                context.Items.Add(ModifierKey, new List<CacheSerializerModifier>());

            requestes = (ICollection<CacheSerializerModifier>)context.Items[ModifierKey]!;

            lock (((ICollection)requestes).SyncRoot)
                requestes.Add(this);
        }

        public void Dispose()
        {
            lock (((ICollection)requestes).SyncRoot)
                requestes.Remove(this);
        }

        // Properties.
        public bool NoCache { get; set; }

        // Static methods.
        public static bool IsNoCacheEnabled(IExecutionContext context)
        {
            if (context.Items is null)
                throw new ExecutionContextNotFoundException();

            if (!context.Items.ContainsKey(ModifierKey))
                return false;
            var requestes = (ICollection<CacheSerializerModifier>)context.Items[ModifierKey]!;

            lock (((ICollection)requestes).SyncRoot)
                return requestes.Any(r => r.NoCache);
        }
    }
}
