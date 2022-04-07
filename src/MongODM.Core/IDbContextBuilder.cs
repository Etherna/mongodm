using Etherna.MongoDB.Driver;
using Etherna.MongODM.Core.Options;
using System.Collections.Generic;

namespace Etherna.MongODM.Core
{
    public interface IDbContextBuilder
    {
        void Initialize(
            IDbDependencies dependencies,
            IMongoClient mongoClient,
            IDbContextOptions options,
            IEnumerable<IDbContext> childDbContexts);
    }
}
