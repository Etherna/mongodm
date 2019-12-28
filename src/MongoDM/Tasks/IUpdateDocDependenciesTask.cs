using Digicando.MongoDM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Digicando.MongoDM.Tasks
{
    public interface IUpdateDocDependenciesTask
    {
        Task RunAsync<TModel, TKey>(
            IEnumerable<string> idPaths,
            TKey modelId)
            where TModel : class, IEntityModel<TKey>;
    }
}