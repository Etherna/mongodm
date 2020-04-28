#nullable enable
namespace Digicando.MongODM
{
    public interface IDbContextInitializable
    {
        bool IsInitialized { get; }

        void Initialize(IDbContext dbContext);
    }
}