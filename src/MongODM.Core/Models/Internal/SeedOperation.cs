namespace Etherna.MongODM.Models.Internal
{
    public class SeedOperation : OperationBase
    {
        // Constructors.
        public SeedOperation(IDbContext owner)
            : base(owner)
        { }
        protected SeedOperation() { }
    }
}
