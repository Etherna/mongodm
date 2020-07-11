namespace Etherna.MongODM.Operations
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
