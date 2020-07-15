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
using System;

namespace Etherna.MongODM.Serialization.Modifiers
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
