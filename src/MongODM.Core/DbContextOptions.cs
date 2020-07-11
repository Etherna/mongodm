using Etherna.MongODM.Serialization;
using System.Linq;

namespace Etherna.MongODM
{
    public class DbContextOptions
    {
        public SemanticVersion ApplicationVersion { get; set; } = "1.0.0";
        public string ConnectionString { get; set; } = "mongodb://localhost/localDb";
        public string DbName => ConnectionString.Split('?')[0]
                                                .Split('/').Last();
        public string DbOperationsCollectionName { get; set; } = "_db_ops";
        public string? Identifier { get; set; }
    }

    public class DbContextOptions<TDbContext> : DbContextOptions
        where TDbContext : class, IDbContext
    { }
}
