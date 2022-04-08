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
        private IReadOnlyDictionary<Type, ICollectionRepository> _collectionRepositoriesByModelType = default!;
        private IReadOnlyDictionary<Type, IGridFSRepository> _gridFSRepositoriesByModelType = default!;
        private IReadOnlyDictionary<Type, IRepository> _repositoriesByModelType = default!;

        // Initializer.
        public void Initialize(IDbContext dbContext)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");
            this.dbContext = dbContext;

            IsInitialized = true;
        }

        // Properties.
        public bool IsInitialized { get; private set; }

        public IReadOnlyDictionary<Type, ICollectionRepository> CollectionRepositoriesByModelType
        {
            get
            {
                if (_collectionRepositoriesByModelType is null)
                {
                    var dbContextType = dbContext.GetType();

                    // Select ICollectionRepository<,> from dbcontext properties.
                    var repos = dbContextType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                        .Where(prop =>
                        {
                            var propType = prop.PropertyType;

                            if (propType.IsGenericType &&
                                propType.GetGenericTypeDefinition() == typeof(ICollectionRepository<,>))
                                return true;

                            if (propType.GetInterfaces()
                                        .Where(@interface => @interface.IsGenericType)
                                        .Select(@interface => @interface.GetGenericTypeDefinition())
                                        .Contains(typeof(ICollectionRepository<,>)))
                                return true;

                            return false;
                        });

                    // Initialize registry.
                    _collectionRepositoriesByModelType = repos.ToDictionary(
                        prop => ((ICollectionRepository)prop.GetValue(dbContext)).GetModelType,
                        prop => (ICollectionRepository)prop.GetValue(dbContext));
                }

                return _collectionRepositoriesByModelType;
            }
        }
        public IReadOnlyDictionary<Type, IGridFSRepository> GridFSRepositoriesByModelType
        {
            get
            {
                if (_gridFSRepositoriesByModelType is null)
                {
                    var dbContextType = dbContext.GetType();

                    //select ICollectionRepository<,> implementing properties
                    var repos = dbContextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(prop =>
                        {
                            var propType = prop.PropertyType;

                            if (propType.IsGenericType &&
                                propType.GetGenericTypeDefinition() == typeof(IGridFSRepository<>))
                                return true;

                            if (propType.GetInterfaces()
                                        .Where(@interface => @interface.IsGenericType)
                                        .Select(@interface => @interface.GetGenericTypeDefinition())
                                        .Contains(typeof(IGridFSRepository<>)))
                                return true;

                            return false;
                        });

                    //construct registry
                    _gridFSRepositoriesByModelType = repos.ToDictionary(
                        prop => ((IGridFSRepository)prop.GetValue(dbContext)).GetModelType,
                        prop => (IGridFSRepository)prop.GetValue(dbContext));
                }

                return _gridFSRepositoriesByModelType;
            }
        }
        public IReadOnlyDictionary<Type, IRepository> RepositoriesByModelType
        {
            get
            {
                if (_repositoriesByModelType is null)
                {
                    var repoMap = new Dictionary<Type, IRepository>();

                    foreach (var pair in CollectionRepositoriesByModelType)
                        repoMap.Add(pair.Key, pair.Value);

                    foreach (var pair in GridFSRepositoriesByModelType)
                        repoMap.Add(pair.Key, pair.Value);

                    _repositoriesByModelType = repoMap;
                }

                return _repositoriesByModelType;
            }
        }
    }
}
