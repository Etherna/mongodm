namespace Etherna.MongODM.Serialization
{
    public interface IModelSerializerCollector
    {
        // Methods.
        void Register(IDbContext dbContext);
    }
}
