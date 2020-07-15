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

using Etherna.MongODM.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Etherna.MongODM.Operations
{
    public abstract class OperationBase : IEntityModel<string>
    {
        // Constructors and dispose.
        public OperationBase(IDbContext owner)
        {
            if (owner is null)
                throw new ArgumentNullException(nameof(owner));

            CreationDateTime = DateTime.Now;
            DbContextName = owner.Identifier;
        }
        protected OperationBase() { }
        public void DisposeForDelete() { }

        // Properties.
        public virtual string Id { get; protected set; } = default!;
        public virtual DateTime CreationDateTime { get; protected set; }
        public virtual string DbContextName { get; protected set; } = default!;

        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter needed for deserialization scope")]
        public virtual IDictionary<string, object>? ExtraElements { get; protected set; }
    }
}
