using Digicando.MongODM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Digicando.MongODM.Tasks
{
    public interface IUpdateDocDependenciesTask
    {
        Task RunAsync<TModel, TKey>(
            IEnumerable<string> idPaths,
            TKey modelId)
            where TModel : class, IEntityModel<TKey>;
    }
}