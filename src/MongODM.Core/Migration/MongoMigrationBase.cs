using System;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Migration
{
    public abstract class MongoMigrationBase
    {
        // Constructors.
        public MongoMigrationBase(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        // Properties.
        public string Id { get; }

        // Methods.
        /// <summary>
        /// Perform migration with optional updating callback
        /// </summary>
        /// <param name="callbackEveryDocuments">Interval of processed documents between callback invokations. 0 if ignore callback</param>
        /// <param name="callbackAsync">The async callback function. Parameter is number of processed documents</param>
        /// <returns>The migration result</returns>
        public abstract Task<MigrationResult> MigrateAsync(int callbackEveryDocuments = 0, Func<long, Task>? callbackAsync = null, CancellationToken cancellationToken = default);
    }
}
