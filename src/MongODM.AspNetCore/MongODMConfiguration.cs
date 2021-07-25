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
using Etherna.MongODM.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Etherna.MongODM.AspNetCore
{
    public class MongODMConfiguration : IMongODMConfiguration, IDisposable
    {
        // Fields.
        private readonly ReaderWriterLockSlim configLock = new(LockRecursionPolicy.SupportsRecursion);
        private readonly List<Type> dbContextTypes = new();
        private bool disposed;
        private readonly IServiceCollection services;

        // Constructor.
        public MongODMConfiguration(
            IServiceCollection services)
        {
            this.services = services;
        }

        // Dispose.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            // Dispose managed resources.
            if (disposing)
                configLock.Dispose();

            disposed = true;
        }

        // Properties.
        public bool IsFrozen { get; private set; }

        // Methods.
        public IMongODMConfiguration AddDbContext<TDbContext>(
            Action<DbContextOptions>? dbContextConfig = null)
            where TDbContext : class, IDbContext =>
            AddDbContext<TDbContext, TDbContext>(dbContextConfig);

        public IMongODMConfiguration AddDbContext<TDbContext, TDbContextImpl>(
            Action<DbContextOptions>? dbContextConfig = null)
            where TDbContext : class, IDbContext
            where TDbContextImpl : class, TDbContext
        {
            configLock.EnterWriteLock();
            try
            {
                if (IsFrozen)
                    throw new InvalidOperationException("Configuration is frozen");

                // Register dbContext.
                services.AddSingleton<TDbContext, TDbContextImpl>(sp =>
                {
                    // Get dependencies.
                    var dependencies = sp.GetRequiredService<IDbDependencies>();
                    var options = new DbContextOptions();
                    dbContextConfig?.Invoke(options);

                    // Initialize instance.
                    var dbContext = Activator.CreateInstance<TDbContextImpl>();
                    if (dbContext is not IDbContextBuilder dbContextBuilder)
                        throw new InvalidOperationException($"DbContext doesn't implement {nameof(IDbContextBuilder)}");

                    var task = dbContextBuilder.InitializeAsync(dependencies, options);
                    task.Wait();

                    return dbContext;
                });

                // Add db context type.
                dbContextTypes.Add(typeof(TDbContext));

                return this;
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }

        public void Freeze(IMongODMOptionsBuilder mongODMOptionsBuilder)
        {
            if (mongODMOptionsBuilder is null)
                throw new ArgumentNullException(nameof(mongODMOptionsBuilder));

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

                // Report configuration to options.
                mongODMOptionsBuilder.SetDbContextTypes(dbContextTypes);
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }
    }
}
