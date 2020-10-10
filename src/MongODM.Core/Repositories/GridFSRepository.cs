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

using Etherna.MongODM.Core.Exceptions;
using Etherna.MongODM.Core.Models;
using Etherna.MongODM.Core.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Repositories
{
    public class GridFSRepository<TModel> :
        RepositoryBase<TModel, string>,
        IGridFSRepository<TModel>
        where TModel : class, IFileModel
    {
        // Fields.
        private readonly GridFSRepositoryOptions<TModel> options;
        private GridFSBucket _gridFSBucket = default!;

        // Constructors.
        public GridFSRepository(string name)
            : this(new GridFSRepositoryOptions<TModel>(name))
        { }

        public GridFSRepository(GridFSRepositoryOptions<TModel> options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        // Properties.
        public IGridFSBucket GridFSBucket =>
            _gridFSBucket ??= new GridFSBucket(DbContext.Database, new GridFSBucketOptions { BucketName = options.Name });
        public override string Name => options.Name;

        // Methods.
        public override Task BuildIndexesAsync(IDocumentSchemaRegister schemaRegister, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public virtual Task<byte[]> DownloadAsBytesAsync(string id, CancellationToken cancellationToken = default) =>
            GridFSBucket.DownloadAsBytesAsync(ObjectId.Parse(id), null, cancellationToken);

        public virtual async Task<Stream> DownloadAsStreamAsync(string id, CancellationToken cancellationToken = default) =>
            await GridFSBucket.OpenDownloadStreamAsync(ObjectId.Parse(id), null, cancellationToken).ConfigureAwait(false);

        // Protected methods.
        protected override async Task CreateOnDBAsync(IEnumerable<TModel> models, CancellationToken cancellationToken)
        {
            if (models is null)
                throw new ArgumentNullException(nameof(models));

            foreach (var model in models)
                await CreateOnDBAsync(model, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task CreateOnDBAsync(TModel model, CancellationToken cancellationToken)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            // Upload.
            model.Stream.Position = 0;
            var id = await GridFSBucket.UploadFromStreamAsync(model.Name, model.Stream, new GridFSUploadOptions
            {
                Metadata = options.MetadataSerializer?.Invoke(model)
            }, cancellationToken).ConfigureAwait(false);
            ReflectionHelper.SetValue(model, m => m.Id, id.ToString());
        }

        protected override Task DeleteOnDBAsync(TModel model, CancellationToken cancellationToken)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            return GridFSBucket.DeleteAsync(ObjectId.Parse(model.Id), cancellationToken);
        }

        protected override async Task<TModel> FindOneOnDBAsync(string id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", ObjectId.Parse(id));
            var mongoFile = await GridFSBucket.Find(filter, cancellationToken: cancellationToken)
                                              .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (mongoFile == null)
                throw new MongodmEntityNotFoundException($"Can't find key {id}");

            var file = DbContext.ProxyGenerator.CreateInstance<TModel>(DbContext);
            ReflectionHelper.SetValue(file, m => m.Id, mongoFile.Id.ToString());
            ReflectionHelper.SetValue(file, m => m.Length, mongoFile.Length);
            ReflectionHelper.SetValue(file, m => m.Name, mongoFile.Filename);

            // Deserialize metadata.
            options.MetadataDeserializer?.Invoke(mongoFile.Metadata, file);

            return file;
        }
    }
}