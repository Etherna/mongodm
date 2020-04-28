using Digicando.MongODM.Models;
using MongoDB.Driver.GridFS;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace Digicando.MongODM.Repositories
{
    public interface IGridFSRepository : IRepository
    {
        IGridFSBucket GridFSBucket { get; }

        Task<byte[]> DownloadAsBytesAsync(string id, CancellationToken cancellationToken = default);

        Task<Stream> DownloadAsStreamAsync(string id, CancellationToken cancellationToken = default);
    }

    public interface IGridFSRepository<TModel> : IRepository<TModel, string>, IGridFSRepository
        where TModel : class, IFileModel
    { }
}