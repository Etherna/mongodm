using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Migration
{
    public abstract class MongoMigrationBase
    {
        public abstract Task MigrateAsync(CancellationToken cancellationToken = default);
    }
}
