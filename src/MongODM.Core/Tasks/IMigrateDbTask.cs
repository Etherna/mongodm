using System.Threading.Tasks;

namespace Etherna.MongODM.Tasks
{
    public interface IMigrateDbTask
    {
        Task RunAsync<TDbContext>()
            where TDbContext : class, IDbContext;
    }
}