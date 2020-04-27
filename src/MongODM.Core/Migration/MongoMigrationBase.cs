using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongODM.Migration
{
    public abstract class MongoMigrationBase
    {
        public abstract Task MigrateAsync(CancellationToken cancellationToken = default);
    }
}
