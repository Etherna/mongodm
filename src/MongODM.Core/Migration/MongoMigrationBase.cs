using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongODM.Migration
{
    public abstract class MongoMigrationBase<TModel>
    {
        public MongoMigrationBase(int priorityIndex)
        {
            PriorityIndex = priorityIndex;
        }

        public int PriorityIndex { get; }

        public abstract Task MigrateAsync(
            IMongoCollection<TModel> sourceCollection,
            CancellationToken cancellationToken = default);
    }
}
