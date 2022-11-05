using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Extensions
{
    /*
     * Always group similar log delegates by type, always use incremental event ids.
     * Last event id is: 17
     */
    public static class LoggerExtensions
    {
        // Fields.
        //*** TRACE LOGS ***
        private static readonly Action<ILogger, string, string, Exception> _repositoryAccessedCollection =
            LoggerMessage.Define<string, string>(
                LogLevel.Trace,
                new EventId(11, nameof(RepositoryAccessedCollection)),
                "Repository {RepositoryName} of DbContext {DbName} accessed collection");

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

        private static readonly Action<ILogger, string, string, Exception> _repositoryBuiltIndexes =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(12, nameof(RepositoryBuiltIndexes)),
                "Repository {RepositoryName} of DbContext {DbName} built indexes");

        private static readonly Action<ILogger, string, string, string, Exception> _repositoryCreatedDocument =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Information,
                new EventId(14, nameof(RepositoryCreatedDocument)),
                "Repository {RepositoryName} of DbContext {DbName} created document with Id: {ModelId}");

        private static readonly Action<ILogger, string, string, IEnumerable<string>, Exception> _repositoryCreatedDocuments =
            LoggerMessage.Define<string, string, IEnumerable<string>>(
                LogLevel.Information,
                new EventId(13, nameof(RepositoryCreatedDocuments)),
                "Repository {RepositoryName} of DbContext {DbName} created multiple documents with Ids: {ModelsId}");

        private static readonly Action<ILogger, string, string, string, Exception> _repositoryDeletedDocument =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Information,
                new EventId(15, nameof(RepositoryDeletedDocument)),
                "Repository {RepositoryName} of DbContext {DbName} deleted document with Id: {ModelId}");

        private static readonly Action<ILogger, string, string, string, Exception> _repositoryFoundDocument =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Information,
                new EventId(16, nameof(RepositoryFoundDocument)),
                "Repository {RepositoryName} of DbContext {DbName} found document with Id: {ModelId}");

        private static readonly Action<ILogger, string, string, string, Exception> _repositoryReplacedDocument =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Information,
                new EventId(17, nameof(RepositoryReplacedDocument)),
                "Repository {RepositoryName} of DbContext {DbName} replaced document with Id: {ModelId}");

        //*** WARNING LOGS ***

        //*** ERROR LOGS ***

        //*** FATAL LOGS ***

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

        public static void RepositoryAccessedCollection(this ILogger logger, string repositoryName, string dbName) =>
            _repositoryAccessedCollection(logger, repositoryName, dbName, null!);

        public static void RepositoryBuiltIndexes(this ILogger logger, string repositoryName, string dbName) =>
            _repositoryBuiltIndexes(logger, repositoryName, dbName, null!);

        public static void RepositoryCreatedDocument(this ILogger logger, string repositoryName, string dbName, string modelId) =>
            _repositoryCreatedDocument(logger, repositoryName, dbName, modelId, null!);

        public static void RepositoryCreatedDocuments(this ILogger logger, string repositoryName, string dbName, IEnumerable<string> modelsId) =>
            _repositoryCreatedDocuments(logger, repositoryName, dbName, modelsId, null!);

        public static void RepositoryDeletedDocument(this ILogger logger, string repositoryName, string dbName, string modelId) =>
            _repositoryDeletedDocument(logger, repositoryName, dbName, modelId, null!);

        public static void RepositoryFoundDocument(this ILogger logger, string repositoryName, string dbName, string modelId) =>
            _repositoryFoundDocument(logger, repositoryName, dbName, modelId, null!);

        public static void RepositoryInitialized(this ILogger logger, string repositoryName, string dbName) =>
            _repositoryInitialized(logger, repositoryName, dbName, null!);

        public static void RepositoryRegistryInitialized(this ILogger logger, string dbName) =>
            _repositoryRegistryInitialized(logger, dbName, null!);

        public static void RepositoryReplacedDocument(this ILogger logger, string repositoryName, string dbName, string modelId) =>
            _repositoryReplacedDocument(logger, repositoryName, dbName, modelId, null!);

        public static void SchemaRegistryInitialized(this ILogger logger, string dbName) =>
            _schemaRegistryInitialized(logger, dbName, null!);
    }
}
