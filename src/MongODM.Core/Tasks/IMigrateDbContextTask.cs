using System.Threading.Tasks;

namespace Etherna.MongODM.Tasks
{
    public interface IMigrateDbContextTask
    {
        Task RunAsync<TDbContext>(string authorId, string taskId)
            where TDbContext : class, IDbContext;
    }
}