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

using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Exceptions;
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Serialization.Mapping;
using MongoDB.Bson.Serialization;
using MoreLinq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Repositories
{
    public abstract class RepositoryBase<TModel, TKey> :
        IRepository<TModel, TKey>
        where TModel : class, IEntityModel<TKey>
    {
        // Initializer.
        public virtual void Initialize(IDbContext dbContext)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");
            DbContext = dbContext;

            IsInitialized = true;
        }

        // Properties.
        public IDbContext DbContext { get; private set; } = default!;
        public Type GetKeyType => typeof(TKey);
        public Type GetModelType => typeof(TModel);
        public bool IsInitialized { get; private set; }
        public abstract string Name { get; }

        // Methods.
        public abstract Task BuildIndexesAsync(ISchemaRegister schemaRegister, CancellationToken cancellationToken = default);

        public virtual async Task CreateAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default)
        {
            await CreateOnDBAsync(models, cancellationToken).ConfigureAwait(false);
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task CreateAsync(TModel model, CancellationToken cancellationToken = default)
        {
            await CreateOnDBAsync(model, cancellationToken).ConfigureAwait(false);
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var model = await FindOneAsync(id, cancellationToken: cancellationToken).ConfigureAwait(false);
            await DeleteAsync(model, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task DeleteAsync(TModel model, CancellationToken cancellationToken = default)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            // Process cascade delete.
            var referencesIdsPaths = DbContext.SchemaRegister.GetReferencedIdMemberMapsFromRootModel(typeof(TModel))
                .Where(d => d.UseCascadeDelete == true)
                .Where(d => d.EntityModelMapPath.Count() == 2) //ignore references of references
                .DistinctBy(d => d.FullPathToString())
                .Select(d => d.MemberPath);

            foreach (var idPath in referencesIdsPaths)
                await CascadeDeleteMembersAsync(model, idPath).ConfigureAwait(false);

            // Unlink dependent models.
            model.DisposeForDelete();
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Delete model.
            await DeleteOnDBAsync(model, cancellationToken).ConfigureAwait(false);

            // Remove from cache.
            if (DbContext.DbCache.LoadedModels.ContainsKey(model.Id!))
                DbContext.DbCache.RemoveModel(model.Id!);
        }

        public async Task DeleteAsync(IEntityModel model, CancellationToken cancellationToken = default)
        {
            if (!(model is TModel castedModel))
                throw new MongodmInvalidEntityTypeException("Invalid model type");
            await DeleteAsync(castedModel, cancellationToken).ConfigureAwait(false);
        }

        public async Task<object> FindOneAsync(object id, CancellationToken cancellationToken = default) =>
            await FindOneAsync((TKey)id, cancellationToken).ConfigureAwait(false);

        public virtual async Task<TModel> FindOneAsync(
            TKey id,
            CancellationToken cancellationToken = default)
        {
            if (DbContext.DbCache.LoadedModels.ContainsKey(id!))
            {
                var cachedModel = DbContext.DbCache.LoadedModels[id!] as TModel;
                if ((cachedModel as IReferenceable)?.IsSummary == false)
                    return cachedModel!;
            }

            return await FindOneOnDBAsync(id, cancellationToken).ConfigureAwait(false);
        }

        public async Task<object?> TryFindOneAsync(object id, CancellationToken cancellationToken = default) =>
            await TryFindOneAsync((TKey)id, cancellationToken).ConfigureAwait(false);

        public async Task<TModel?> TryFindOneAsync(
            TKey id,
            CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return null;
            }

            try
            {
                return await FindOneAsync(id, cancellationToken).ConfigureAwait(false);
            }
            catch (MongodmEntityNotFoundException)
            {
                return null;
            }
        }

        // Protected abstract methods.
        protected abstract Task CreateOnDBAsync(IEnumerable<TModel> models, CancellationToken cancellationToken);

        protected abstract Task CreateOnDBAsync(TModel model, CancellationToken cancellationToken);

        protected abstract Task DeleteOnDBAsync(TModel model, CancellationToken cancellationToken);

        protected abstract Task<TModel> FindOneOnDBAsync(TKey id, CancellationToken cancellationToken = default);

        // Helpers.
        private async Task CascadeDeleteMembersAsync(object currentModel, IEnumerable<BsonMemberMap> idPath)
        {
            if (!idPath.Any())
                throw new ArgumentException("Member path can't be empty", nameof(idPath));

            var currentMember = idPath.First();
            var memberTail = idPath.Skip(1);

            if (currentMember.IsIdMember())
            {
                //cascade delete model
                var repository = DbContext.RepositoryRegister.ModelRepositoryMap[currentModel.GetType().BaseType];
                try { await repository.DeleteAsync((IEntityModel)currentModel).ConfigureAwait(false); }
#pragma warning disable CA1031 // Do not catch general exception types. Internal exceptions thrown by MongoDB drivers
                catch { }
#pragma warning restore CA1031 // Do not catch general exception types
            }
            else
            {
                //recursion on value
                var memberInfo = currentMember.MemberInfo;
                var memberValue = ReflectionHelper.GetValue(currentModel, memberInfo);
                if (memberValue == null)
                    return;

                if (memberValue is IEnumerable enumerableMemberValue) //if enumerable
                {
                    if (enumerableMemberValue is IDictionary dictionaryMemberValue)
                        enumerableMemberValue = dictionaryMemberValue.Values;

                    foreach (var itemValue in enumerableMemberValue.Cast<object>().ToArray())
                        await CascadeDeleteMembersAsync(itemValue, memberTail).ConfigureAwait(false);
                }
                else
                {
                    await CascadeDeleteMembersAsync(memberValue, memberTail).ConfigureAwait(false);
                }
            }
        }
    }
}