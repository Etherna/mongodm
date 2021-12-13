using Etherna.MongODM.Core.Options;

namespace Etherna.MongODM.Core
{
    public interface IDbContextBuilder
    {
        void Initialize(IDbDependencies dependencies, IDbContextOptions options);
    }
}
