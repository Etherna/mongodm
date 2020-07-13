using System;

namespace Etherna.MongODM.Models.Internal
{
    public abstract class OperationBase : EntityModelBase<string>
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

        // Properties.
        public virtual string DbContextName { get; protected set; } = default!;
    }
}
