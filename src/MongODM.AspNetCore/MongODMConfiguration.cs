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

using Etherna.MongODM.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Etherna.MongODM.AspNetCore
{
    public class MongODMConfiguration : IMongODMConfiguration, IDisposable
    {
        // Fields.
        private readonly ReaderWriterLockSlim configLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly IServiceCollection services;
        private readonly List<Type> _dbContextTypes = new List<Type>();

        // Constructor and dispose.
        public MongODMConfiguration(IServiceCollection services)
        {
            this.services = services;
        }

        public void Dispose()
        {
            configLock.Dispose();
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
                    throw new InvalidOperationException("Register is frozen");

                // Register dbContext.
                services.AddSingleton<TDbContext>();

                // Register options.
                var contextOptions = new DbContextOptions<TDbContext>();
                dbContextConfig?.Invoke(contextOptions);
                services.AddSingleton(contextOptions);

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
                    throw new InvalidOperationException("Register is frozen");

                // Register dbContext.
                services.AddSingleton<TDbContext, TDbContextImpl>();
                services.AddSingleton(sp => sp.GetService<TDbContext>() as TDbContextImpl);

                // Register options.
                var contextOptions = new DbContextOptions<TDbContextImpl>();
                dbContextConfig?.Invoke(contextOptions);
                services.AddSingleton(contextOptions);

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
    }
}
