// Copyright 2020-present Etherna SA
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Etherna.ExecContext;
using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Etherna.MongODM.Core.Utility
{
    public class DbCache : IDbCache
    {
        // Consts.
        private const string CacheKeyPrefix = "DBCache-";

        // Fields.
        private string cacheKey = default!;
        private IExecutionContext executionContext = default!;
        private ILogger logger = default!;

        // Constructors.
        public void Initialize(IDbContext dbContext, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(dbContext, nameof(dbContext));
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");

            var cacheKeyBuilder = new StringBuilder(CacheKeyPrefix);
            cacheKeyBuilder.Append(dbContext.Identifier);
            cacheKey = cacheKeyBuilder.ToString();
            executionContext = dbContext.ExecutionContext;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            IsInitialized = true;

            this.logger.DbCacheInitialized(dbContext.Options.DbName);
        }

        // Properties.
        public bool IsInitialized { get; private set; }
        public IReadOnlyDictionary<object, IEntityModel> LoadedModels
        {
            get
            {
                if (executionContext.Items is null)
                    throw new InvalidOperationException("Execution context can't have null Items here");

                lock (executionContext.Items)
                    return GetScopedCache();
            }
        }

        // Methods.
        public void ClearCache()
        {
            if (executionContext.Items is null)
                throw new InvalidOperationException("Execution context can't have null Items here");

            lock (executionContext.Items)
                GetScopedCache().Clear();
        }

        public void AddModel<TModel>(object id, TModel model)
            where TModel : class, IEntityModel
        {
            ArgumentNullException.ThrowIfNull(id, nameof(id));
            ArgumentNullException.ThrowIfNull(model, nameof(model));
            if (executionContext.Items is null)
                throw new InvalidOperationException("Execution context can't have null Items here");

            lock (executionContext.Items)
                GetScopedCache().Add(id, model);
        }

        public void RemoveModel(object id)
        {
            if (executionContext.Items is null)
                throw new InvalidOperationException("Execution context can't have null Items here");

            lock (executionContext.Items)
                GetScopedCache().Remove(id);
        }

        // Helpers.
        private Dictionary<object, IEntityModel> GetScopedCache()
        {
            if (executionContext.Items is null)
                throw new InvalidOperationException("Execution context can't have null Items here");

            if (!executionContext.Items.ContainsKey(cacheKey))
                executionContext.Items.Add(cacheKey, new Dictionary<object, IEntityModel>());

            return (Dictionary<object, IEntityModel>)executionContext.Items[cacheKey]!;
        }
    }
}
