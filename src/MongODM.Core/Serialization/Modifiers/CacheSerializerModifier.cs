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

using Etherna.ExecContext;
using Etherna.ExecContext.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Modifiers
{
    class CacheSerializerModifier : IDisposable
    {
        // Consts.
        private const string ModifierKey = "CacheSerializerModifier";

        // Fields.
        private readonly ICollection<CacheSerializerModifier> requestes;

        // Constructors and dispose.
        public CacheSerializerModifier(IExecutionContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (context.Items is null)
                throw new ExecutionContextNotFoundException();

            if (!context.Items.ContainsKey(ModifierKey))
                context.Items.Add(ModifierKey, new List<CacheSerializerModifier>());

            requestes = (ICollection<CacheSerializerModifier>)context.Items[ModifierKey];

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
            var requestes = (ICollection<CacheSerializerModifier>)context.Items[ModifierKey];

            lock (((ICollection)requestes).SyncRoot)
                return requestes.Any(r => r.NoCache);
        }
    }
}
