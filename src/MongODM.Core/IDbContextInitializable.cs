namespace Etherna.MongODM
{
    public interface IDbContextInitializable
    {
        bool IsInitialized { get; }

        void Initialize(IDbContext dbContext);
    }
}