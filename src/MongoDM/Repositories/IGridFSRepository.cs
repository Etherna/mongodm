using Digicando.MongoDM.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongoDM.Repositories
{
    public interface IGridFSRepository : IRepository
    {
        Task<byte[]> DownloadAsBytesAsync(string id, CancellationToken cancellationToken = default);

        Task<Stream> DownloadAsStreamAsync(string id, CancellationToken cancellationToken = default);
    }

    public interface IGridFSRepository<TModel> : IRepository<TModel, string>, IGridFSRepository
        where TModel : class, IFileModel
    { }
}