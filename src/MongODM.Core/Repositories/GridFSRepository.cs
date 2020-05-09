﻿using Digicando.DomainHelper;
using Digicando.MongODM.Exceptions;
using Digicando.MongODM.Models;
using Digicando.MongODM.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongODM.Repositories
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
        public IGridFSBucket GridFSBucket => _gridFSBucket ??
            (_gridFSBucket = new GridFSBucket(DbContext.Database, new GridFSBucketOptions { BucketName = options.Name }));

        // Methods.
        public override Task BuildIndexesAsync(IDocumentSchemaRegister schemaRegister, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public virtual Task<byte[]> DownloadAsBytesAsync(string id, CancellationToken cancellationToken = default) =>
            GridFSBucket.DownloadAsBytesAsync(ObjectId.Parse(id), null, cancellationToken);

        public virtual async Task<Stream> DownloadAsStreamAsync(string id, CancellationToken cancellationToken = default) =>
            await GridFSBucket.OpenDownloadStreamAsync(ObjectId.Parse(id), null, cancellationToken);

        // Protected methods.
        protected override async Task CreateOnDBAsync(IEnumerable<TModel> models, CancellationToken cancellationToken)
        {
            foreach (var model in models)
                await CreateOnDBAsync(model, cancellationToken);
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
            });
            ReflectionHelper.SetValue(model, m => m.Id, id.ToString());
        }

        protected override Task DeleteOnDBAsync(TModel model, CancellationToken cancellationToken) =>
            GridFSBucket.DeleteAsync(ObjectId.Parse(model.Id), cancellationToken);

        protected override async Task<TModel> FindOneOnDBAsync(string id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", ObjectId.Parse(id));
            var mongoFile = await GridFSBucket.Find(filter).SingleOrDefaultAsync(cancellationToken);
            if (mongoFile == null)
                throw new EntityNotFoundException($"Can't find key {id}");

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