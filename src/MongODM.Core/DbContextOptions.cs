using Etherna.MongODM.Serialization;
using System.Linq;

namespace Etherna.MongODM
{
    public class DbContextOptions
    {
        public string ConnectionString { get; set; } = "mongodb://localhost/localDb";
        public string DBName => ConnectionString.Split('?')[0]
                                                .Split('/').Last();
        public DocumentVersion DocumentVersion { get; set; } = "1.0.0";
    }

    public class DbContextOptions<TDbContext> : DbContextOptions
        where TDbContext : class, IDbContext
    { }
}
