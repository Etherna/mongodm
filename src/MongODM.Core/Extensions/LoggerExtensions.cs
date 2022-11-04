using Microsoft.Extensions.Logging;
using System;

namespace Etherna.MongODM.Core.Extensions
{
    /*
     * Always group similar log delegates by type, always use incremental event ids.
     * Last event id is: 10
     */
    public static class LoggerExtensions
    {
        // Fields.
        //*** DEBUG LOGS ***
        private static readonly Action<ILogger, string, Exception> _dbCacheInitialized =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(7, nameof(DbCacheInitialized)),
                "DbCache of DbContext {DbName} initialized");

        private static readonly Action<ILogger, string, string, string, Exception> _dbContextSavedChangedModelToRepository =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Debug,
                new EventId(3, nameof(DbContextSavedChangedModelToRepository)),
                "DbContext {DbName} saved changed model with Id {ModelId} on repository {RepositoryName}");

        private static readonly Action<ILogger, string, Exception> _dbMaintainerInitialized =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(8, nameof(DbMaintainerInitialized)),
                "DbMaintainer of DbContext {DbName} initialized");

        private static readonly Action<ILogger, string, Exception> _dbMigrationManagerInitialized =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(9, nameof(DbMigrationManagerInitialized)),
                "DbMigrationManager of DbContext {DbName} initialized");

        private static readonly Action<ILogger, string, Exception> _discriminatorRegistryInitialized =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(4, nameof(DiscriminatorRegistryInitialized)),
                "DiscriminatorRegistry of DbContext {DbName} initialized");

        private static readonly Action<ILogger, string, string, Exception> _repositoryInitialized =
            LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                new EventId(10, nameof(RepositoryInitialized)),
                "Repository {RepositoryName} of DbContext {DbName} initialized");

        private static readonly Action<ILogger, string, Exception> _repositoryRegistryInitialized =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(5, nameof(RepositoryRegistryInitialized)),
                "RepositoryRegistry of DbContext {DbName} initialized");

        private static readonly Action<ILogger, string, Exception> _schemaRegistryInitialized =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(6, nameof(SchemaRegistryInitialized)),
                "SchemaRegistry of DbContext {DbName} initialized");

        //*** INFORMATION LOGS ***
        private static readonly Action<ILogger, string, Exception> _dbContextInitialized =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(0, nameof(DbContextInitialized)),
                "DbContext {DbName} initialized");

        private static readonly Action<ILogger, string, Exception> _dbContextSavedChanges =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(DbContextSavedChanges)),
                "DbContext {DbName} saved changes");

        private static readonly Action<ILogger, string, Exception> _dbContextSeeded =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(2, nameof(DbContextSeeded)),
                "DbContext {DbName} has been seeded");

        //*** WARNING LOGS ***

        //*** ERROR LOGS ***

        // Methods.
        public static void DbCacheInitialized(this ILogger logger, string dbName) =>
            _dbCacheInitialized(logger, dbName, null!);

        public static void DbContextInitialized(this ILogger logger, string dbName) =>
            _dbContextInitialized(logger, dbName, null!);

        public static void DbContextSavedChangedModelToRepository(this ILogger logger, string dbName, string modelId, string repositoryName) =>
            _dbContextSavedChangedModelToRepository(logger, dbName, modelId, repositoryName, null!);

        public static void DbContextSavedChanges(this ILogger logger, string dbName) =>
            _dbContextSavedChanges(logger, dbName, null!);

        public static void DbContextSeeded(this ILogger logger, string dbName) =>
            _dbContextSeeded(logger, dbName, null!);

        public static void DbMaintainerInitialized(this ILogger logger, string dbName) =>
            _dbMaintainerInitialized(logger, dbName, null!);

        public static void DbMigrationManagerInitialized(this ILogger logger, string dbName) =>
            _dbMigrationManagerInitialized(logger, dbName, null!);

        public static void DiscriminatorRegistryInitialized(this ILogger logger, string dbName) =>
            _discriminatorRegistryInitialized(logger, dbName, null!);

        public static void RepositoryInitialized(this ILogger logger, string repositoryName, string dbName) =>
            _repositoryInitialized(logger, repositoryName, dbName, null!);

        public static void RepositoryRegistryInitialized(this ILogger logger, string dbName) =>
            _repositoryRegistryInitialized(logger, dbName, null!);

        public static void SchemaRegistryInitialized(this ILogger logger, string dbName) =>
            _schemaRegistryInitialized(logger, dbName, null!);
    }
}
