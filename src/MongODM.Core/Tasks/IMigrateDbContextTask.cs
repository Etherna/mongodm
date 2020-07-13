using System.Threading.Tasks;

namespace Etherna.MongODM.Tasks
{
    public interface IMigrateDbContextTask
    {
        Task RunAsync<TDbContext>(string authorId)
            where TDbContext : class, IDbContext;
    }
}