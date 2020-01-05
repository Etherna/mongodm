namespace Digicando.MongODM.Serialization
{
    public interface IModelSerializerCollector
    {
        // Methods.
        void Register(IDbContext dbContext);
    }
}
