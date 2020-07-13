using Etherna.MongODM.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Etherna.MongODM.Operations
{
    public abstract class OperationBase : IEntityModel<string>
    {
        // Constructors and dispose.
        public OperationBase(IDbContext dbContext)
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));

            CreationDateTime = DateTime.Now;
            DbContextName = dbContext.Identifier;
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
