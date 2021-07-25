using Etherna.MongODM.Core.Options;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core
{
    public interface IDbContextBuilder
    {
        Task InitializeAsync(IDbDependencies dependencies, IDbContextOptions options);
    }
}
