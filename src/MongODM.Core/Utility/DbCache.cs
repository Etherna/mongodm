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

using Etherna.ExecContext;
using Etherna.MongODM.Models;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Utility
{
    public class DbCache : IDbCache
    {
        // Consts.
        private const string CacheKey = "DBCache";

        // Fields.
        private readonly IExecutionContext executionContext;

        // Constructors.
        public DbCache(IExecutionContext executionContext)
        {
            this.executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        }

        // Properties.
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
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (model == null)
                throw new ArgumentNullException(nameof(model));
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

            if (!executionContext.Items.ContainsKey(CacheKey))
                executionContext.Items.Add(CacheKey, new Dictionary<object, IEntityModel>());

            return (Dictionary<object, IEntityModel>)executionContext.Items[CacheKey];
        }
    }
}
