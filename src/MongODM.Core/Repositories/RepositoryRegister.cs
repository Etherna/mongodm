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

namespace Etherna.MongODM.Repositories
{
    public class RepositoryRegister : IRepositoryRegister
    {
        // Fields.
        private IDbContext dbContext = default!;
        private IReadOnlyDictionary<Type, ICollectionRepository> _modelCollectionRepositoryMap = default!;
        private IReadOnlyDictionary<Type, IGridFSRepository> _modelGridFSRepositoryMap = default!;
        private IReadOnlyDictionary<Type, IRepository> _modelRepositoryMap = default!;

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

        public IReadOnlyDictionary<Type, ICollectionRepository> ModelCollectionRepositoryMap
        {
            get
            {
                if (_modelCollectionRepositoryMap is null)
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

                    // Initialize register.
                    _modelCollectionRepositoryMap = repos.ToDictionary(
                        prop => ((ICollectionRepository)prop.GetValue(dbContext)).GetModelType,
                        prop => (ICollectionRepository)prop.GetValue(dbContext));
                }

                return _modelCollectionRepositoryMap;
            }
        }
        public IReadOnlyDictionary<Type, IGridFSRepository> ModelGridFSRepositoryMap
        {
            get
            {
                if (_modelGridFSRepositoryMap is null)
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

                    //construct register
                    _modelGridFSRepositoryMap = repos.ToDictionary(
                        prop => ((IGridFSRepository)prop.GetValue(dbContext)).GetModelType,
                        prop => (IGridFSRepository)prop.GetValue(dbContext));
                }

                return _modelGridFSRepositoryMap;
            }
        }
        public IReadOnlyDictionary<Type, IRepository> ModelRepositoryMap
        {
            get
            {
                if (_modelRepositoryMap is null)
                {
                    var repoMap = new Dictionary<Type, IRepository>();

                    foreach (var pair in ModelCollectionRepositoryMap)
                        repoMap.Add(pair.Key, pair.Value);

                    foreach (var pair in ModelGridFSRepositoryMap)
                        repoMap.Add(pair.Key, pair.Value);

                    _modelRepositoryMap = repoMap;
                }

                return _modelRepositoryMap;
            }
        }
    }
}
