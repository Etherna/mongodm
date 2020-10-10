using Etherna.MongODM.AspNetCoreSample.Models;
using Etherna.MongODM.Core;
using Etherna.MongODM.Core.Repositories;

namespace Etherna.MongODM.AspNetCoreSample.Persistence
{
    public interface ISampleDbContext : IDbContext
    {
        ICollectionRepository<Cat, string> Cats { get; }
    }
}