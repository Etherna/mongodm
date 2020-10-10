//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Etherna.MongODM.Core.Models;
using MongoDB.Driver.GridFS;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Repositories
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