using Digicando.DomainHelper;
using Digicando.MongoDM.Migration;
using Digicando.MongoDM.Models;
using Digicando.MongoDM.ProxyModels;
using Digicando.MongoDM.Serialization;
using MoreLinq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongoDM.Repositories
{
    public abstract class RepositoryBase<TModel, TKey> :
        IRepository<TModel, TKey>
        where TModel : class, IEntityModel<TKey>
    {
        // Fields.
        private readonly IDbContext dbContext;

        // Constructors.
        public RepositoryBase(IDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // Properties.
        public virtual MongoMigrationBase MigrationInfo { get; }

        // Public methods.
        public abstract Task BuildIndexesAsync(IDocumentSchemaRegister schemaRegister);

        public virtual async Task CreateAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default)
        {
            await CreateOnDBAsync(models, cancellationToken);
            await dbContext.SaveChangesAsync();
        }

        public virtual async Task CreateAsync(TModel model, CancellationToken cancellationToken = default)
        {
            await CreateOnDBAsync(model, cancellationToken);
            await dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var model = await FindOneAsync(id, cancellationToken: cancellationToken);
            await DeleteAsync(model, cancellationToken);
        }

        public virtual async Task DeleteAsync(TModel model, CancellationToken cancellationToken = default)
        {
            // Process cascade delete.
            var referencesIdsPaths = dbContext.DocumentSchemaRegister.GetModelEntityReferencesIds(typeof(TModel))
                .Where(d => d.UseCascadeDelete == true)
                .Where(d => d.EntityClassMapPath.Count() == 2) //ignore references of references
                .DistinctBy(d => d.FullPathToString())
                .Select(d => d.MemberPath);

            foreach (var idPath in referencesIdsPaths)
                await CascadeDeleteMembersAsync(model, idPath);

            // Unlink dependent models.
            model.DisposeForDelete();
            await dbContext.SaveChangesAsync();

            // Delete model.
            await DeleteOnDBAsync(model, cancellationToken);

            // Remove from cache.
            if (dbContext.DBCache.LoadedModels.ContainsKey(model.Id))
                dbContext.DBCache.RemoveModel(model.Id);
        }

        public async Task DeleteAsync(IEntityModel model, CancellationToken cancellationToken = default)
        {
            if (!(model is TModel castedModel))
                throw new ArgumentException("Invalid model type");
            await DeleteAsync(castedModel, cancellationToken);
        }

        public virtual async Task<TModel> FindOneAsync(
            TKey id,
            CancellationToken cancellationToken = default)
        {
            if (dbContext.DBCache.LoadedModels.ContainsKey(id))
            {
                var cachedModel = dbContext.DBCache.LoadedModels[id] as TModel;
                if ((cachedModel as IReferenceable)?.IsSummary == false)
                    return cachedModel;
            }

            return await FindOneOnDBAsync(id, cancellationToken);
        }

        public async Task<TModel> TryFindOneAsync(
            TKey id,
            CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return null;
            }

            try
            {
                return await FindOneAsync(id, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        // Protected methods.
        protected abstract Task CreateOnDBAsync(IEnumerable<TModel> models, CancellationToken cancellationToken);

        protected abstract Task CreateOnDBAsync(TModel model, CancellationToken cancellationToken);

        protected abstract Task DeleteOnDBAsync(TModel model, CancellationToken cancellationToken);

        protected abstract Task<TModel> FindOneOnDBAsync(TKey id, CancellationToken cancellationToken = default);

        // Helpers.
        private async Task CascadeDeleteMembersAsync(object currentModel, IEnumerable<EntityMember> idPath)
        {
            if (!idPath.Any())
                throw new ArgumentException("Member path can't be emty", nameof(idPath));

            var currentMember = idPath.First();
            var memberTail = idPath.Skip(1);

            if (currentMember.IsId)
            {
                //cascade delete model
                var repository = dbContext.ModelRepositoryMap[currentModel.GetType().BaseType];
                try { await repository.DeleteAsync(currentModel as IEntityModel); }
                catch { }
            }
            else
            {
                //recursion on value
                var memberInfo = currentMember.MemberMap.MemberInfo;
                var memberValue = ReflectionHelper.GetValue(currentModel, memberInfo);
                if (memberValue == null)
                    return;

                if (memberValue is IEnumerable enumerableMemberValue) //if enumerable
                {
                    if (enumerableMemberValue is IDictionary dictionaryMemberValue)
                        enumerableMemberValue = dictionaryMemberValue.Values;

                    foreach (var itemValue in enumerableMemberValue.Cast<object>().ToArray())
                        await CascadeDeleteMembersAsync(itemValue, memberTail);
                }
                else
                {
                    await CascadeDeleteMembersAsync(memberValue, memberTail);
                }
            }
        }
    }
}
