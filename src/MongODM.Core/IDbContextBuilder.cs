using Etherna.MongODM.Core.Options;
using System.Collections.Generic;

namespace Etherna.MongODM.Core
{
    public interface IDbContextBuilder
    {
        void Initialize(
            IDbDependencies dependencies,
            IDbContextOptions options,
            IEnumerable<IDbContext> childDbContexts);
    }
}
