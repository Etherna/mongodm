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

namespace Etherna.MongODM.Core.Models
{
    public abstract class OperationBase : EntityModelBase<string>
    {
        // Constructors and dispose.
        protected OperationBase(IDbContext dbContext)
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));

            CreationDateTime = DateTime.Now;
            DbContextName = dbContext.Identifier;
        }
        protected OperationBase() { }

        // Properties.
        public virtual string DbContextName { get; protected set; } = default!;
    }
}
