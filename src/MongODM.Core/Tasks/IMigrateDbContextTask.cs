using System.Threading.Tasks;

namespace Etherna.MongODM.Tasks
{
    public interface IMigrateDbContextTask
    {
        Task RunAsync<TDbContext>(string dbMigrationOpId, string taskId)
            where TDbContext : class, IDbContext;
    }
}