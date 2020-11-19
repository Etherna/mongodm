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

using Etherna.MongODM.Core.Options;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Etherna.MongODM.Core
{
    public abstract class MongODMConfiguration : IMongODMConfiguration, IDisposable
    {
        // Fields.
        private readonly ReaderWriterLockSlim configLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly List<Type> _dbContextTypes = new List<Type>();

        // Constructor and dispose.
        public void Dispose()
        {
            configLock.Dispose();
            GC.SuppressFinalize(this);
        }

        // Properties.
        public IEnumerable<Type> DbContextTypes
        {
            get
            {
                Freeze();
                return _dbContextTypes;
            }
        }

        public bool IsFrozen { get; private set; }

        // Methods.

        public IMongODMConfiguration AddDbContext<TDbContext>(
            Action<DbContextOptions<TDbContext>>? dbContextConfig = null)
            where TDbContext : class, IDbContext
        {
            configLock.EnterWriteLock();
            try
            {
                if (IsFrozen)
                    throw new InvalidOperationException("Configuration is frozen");

                // Register dbContext.
                RegisterSingleton<TDbContext>();

                // Register options.
                var contextOptions = new DbContextOptions<TDbContext>();
                dbContextConfig?.Invoke(contextOptions);
                RegisterSingleton(contextOptions);

                // Add db context type.
                _dbContextTypes.Add(typeof(TDbContext));

                return this;
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }

        public IMongODMConfiguration AddDbContext<TDbContext, TDbContextImpl>(
            Action<DbContextOptions<TDbContextImpl>>? dbContextConfig = null)
            where TDbContext : class, IDbContext
            where TDbContextImpl : class, TDbContext
        {
            configLock.EnterWriteLock();
            try
            {
                if (IsFrozen)
                    throw new InvalidOperationException("Configuration is frozen");

                // Register dbContext.
                RegisterSingleton<TDbContext, TDbContextImpl>();
                RegisterSingleton(sp => (TDbContextImpl)sp.GetService(typeof(TDbContext)));

                // Register options.
                var contextOptions = new DbContextOptions<TDbContextImpl>();
                dbContextConfig?.Invoke(contextOptions);
                RegisterSingleton(contextOptions);

                // Add db context type.
                _dbContextTypes.Add(typeof(TDbContext));

                return this;
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }

        public void Freeze()
        {
            configLock.EnterReadLock();
            try
            {
                if (IsFrozen) return;
            }
            finally
            {
                configLock.ExitReadLock();
            }

            configLock.EnterWriteLock();
            try
            {
                // Freeze.
                IsFrozen = true;
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }

        // Abstract protected methods.
        protected abstract void RegisterSingleton<TService>()
             where TService : class;

        protected abstract void RegisterSingleton<TService>(TService instance)
             where TService : class;

        protected abstract void RegisterSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        protected abstract void RegisterSingleton<TService>(Func<IServiceProvider, TService> implementationFactory)
            where TService : class;
    }
}
