namespace Digicando.MongoDM.Serialization
{
    public interface IModelSerializerCollector
    {
        // Methods.
        void Register(IDbContext dbContext);
    }
}
