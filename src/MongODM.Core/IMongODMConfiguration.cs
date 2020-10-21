using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core
{
    public interface IMongODMConfiguration
    {
        IEnumerable<Type> DbContextTypes { get; }
        bool IsFrozen { get; }

        // Methods.
        IMongODMConfiguration AddDbContext<TDbContext>(
            Action<DbContextOptions<TDbContext>>? dbContextConfig = null)
            where TDbContext : class, IDbContext;

        IMongODMConfiguration AddDbContext<TDbContext, TDbContextImpl>(
            Action<DbContextOptions<TDbContextImpl>>? dbContextConfig = null)
            where TDbContext : class, IDbContext
            where TDbContextImpl : class, TDbContext;

        /// <summary>
        /// Freeze configuration.
        /// </summary>
        void Freeze();
    }
}
