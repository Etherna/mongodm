using System.Threading.Tasks;

namespace Digicando.MongoDM.Migration
{
    public abstract class MongoMigrationBase
    {
        public MongoMigrationBase(int priorityIndex)
        {
            PriorityIndex = priorityIndex;
        }

        public int PriorityIndex { get; }

        public abstract Task MigrateAsync();
    }
}
