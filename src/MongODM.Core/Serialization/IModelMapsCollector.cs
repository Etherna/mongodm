namespace Etherna.MongODM.Serialization
{
    public interface IModelMapsCollector
    {
        // Methods.
        void Register(IDbContext dbContext);
    }
}
