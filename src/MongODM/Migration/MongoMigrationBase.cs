using System.Threading.Tasks;

namespace Digicando.MongODM.Migration
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
