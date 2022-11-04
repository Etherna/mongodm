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

using Etherna.MongODM.Core.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Etherna.MongODM.Core.Repositories
{
    public class RepositoryRegistry : IRepositoryRegistry
    {
        // Fields.
        private IDbContext dbContext = default!;
        private ILogger logger = default!;
        private IReadOnlyDictionary<Type, IRepository> _repositoriesByModelType = default!;

        // Initializer.
        public void Initialize(IDbContext dbContext, ILogger logger)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            IsInitialized = true;

            this.logger.RepositoryRegistryInitialized(dbContext.Options.DbName);
        }

        // Properties.
        public bool IsInitialized { get; private set; }

        public IReadOnlyDictionary<Type, IRepository> RepositoriesByModelType
        {
            get
            {
                if (_repositoriesByModelType is null)
                {
                    var dbContextType = dbContext.GetType();

                    // Select ICollectionRepository<,> from dbcontext properties.
                    var repos = dbContextType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                        .Where(prop =>
                        {
                            var propType = prop.PropertyType;

                            if (propType.IsGenericType &&
                                propType.GetGenericTypeDefinition() == typeof(IRepository<,>))
                                return true;

                            if (propType.GetInterfaces()
                                        .Where(@interface => @interface.IsGenericType)
                                        .Select(@interface => @interface.GetGenericTypeDefinition())
                                        .Contains(typeof(IRepository<,>)))
                                return true;

                            return false;
                        });

                    // Initialize registry.
                    _repositoriesByModelType = repos.ToDictionary(
                        prop => ((IRepository)prop.GetValue(dbContext)).GetModelType,
                        prop => (IRepository)prop.GetValue(dbContext));
                }

                return _repositoriesByModelType;
            }
        }
    }
}
