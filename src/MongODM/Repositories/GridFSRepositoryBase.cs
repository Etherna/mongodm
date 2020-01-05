using Digicando.DomainHelper;
using Digicando.MongODM.Models;
using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongODM.Repositories
{
    public abstract class GridFSRepositoryBase<TModel> :
        RepositoryBase<TModel, string>,
        IGridFSRepository<TModel>
        where TModel : class, IFileModel
    {
        // Constructors.
        public GridFSRepositoryBase(
            string bucketName,
            IDbContext dbContext)
            : base(dbContext)
        {
            var bucketOptions = new GridFSBucketOptions();
            if (bucketName != null)
            {
                bucketOptions.BucketName = bucketName;
            }

            GridFSBucket = new GridFSBucket(dbContext.Database, bucketOptions);
            ProxyGenerator = dbContext.ProxyGenerator;
        }

        // Properties.
        protected IGridFSBucket GridFSBucket { get; }
        protected IProxyGenerator ProxyGenerator { get; }

        // Methods.
        public override Task BuildIndexesAsync(IDocumentSchemaRegister schemaRegister) => Task.CompletedTask;

        public virtual Task<byte[]> DownloadAsBytesAsync(string id, CancellationToken cancellationToken = default) =>
            GridFSBucket.DownloadAsBytesAsync(ObjectId.Parse(id), null, cancellationToken);

        public virtual async Task<Stream> DownloadAsStreamAsync(string id, CancellationToken cancellationToken = default) =>
            await GridFSBucket.OpenDownloadStreamAsync(ObjectId.Parse(id), null, cancellationToken);

        // Protected methods.
        protected virtual void CloneMetadataData(TModel src, TModel dest)
        { }

        protected override async Task CreateOnDBAsync(IEnumerable<TModel> models, CancellationToken cancellationToken)
        {
            foreach (var model in models)
                await CreateOnDBAsync(model, cancellationToken);
        }

        protected override async Task CreateOnDBAsync(TModel model, CancellationToken cancellationToken)
        {
            model.Stream.Position = 0;
            var id = await GridFSBucket.UploadFromStreamAsync(model.Name, model.Stream, new GridFSUploadOptions
            {
                Metadata = SerializeMetadata(model)
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
                throw new KeyNotFoundException($"Can't find key {id}");

            var file = ProxyGenerator.CreateInstance<TModel>();
            ReflectionHelper.SetValue(file, m => m.Id, mongoFile.Id.ToString());
            ReflectionHelper.SetValue(file, m => m.Length, mongoFile.Length);
            ReflectionHelper.SetValue(file, m => m.Name, mongoFile.Filename);

            // Deserialize metadata.
            DeserializeMetadata(file, mongoFile.Metadata);

            return file;
        }

        // Private helpers.
        private void DeserializeMetadata(TModel obj, BsonDocument metadata)
        {
            if (metadata != null)
            {
                var metadataObject = BsonSerializer.Deserialize<TModel>(metadata);
                CloneMetadataData(metadataObject, obj);
            }
        }

        private BsonDocument SerializeMetadata(TModel obj)
        {
            if (obj == null)
            {
                return null;
            }

            var document = new BsonDocument();
            BsonSerializer.Serialize(new BsonDocumentWriter(document), obj);
            return document;
        }
    }
}
