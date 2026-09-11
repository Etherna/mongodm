// Copyright 2020-present Etherna SA
// This file is part of Scrinium.
//
// Scrinium is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
//
// Scrinium is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along with Scrinium.
// If not, see <https://www.gnu.org/licenses/>.

using Etherna.MongoDB.Bson.IO;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.Scrinium.Core.Domain.Models;
using Etherna.Scrinium.Core.Domain.Models.DbMigrationOpAgg;
using Etherna.Scrinium.Core.ExecContext.AsyncLocal;
using Etherna.Scrinium.Core.Serialization.Serializers;
using Etherna.Scrinium.Core.Utility;
using Etherna.Scrinium.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace Etherna.Scrinium.IntegrationTests
{
    /* The operations collection of a long lived application carries the documents that every
     * version of the library wrote into it, and they all deserialize through the internal model
     * maps of the engine. The source documents below are listed from the newest version down,
     * each one commented with its schema id and the version that wrote it. */
    [Collection("Integration")]
    public class DbMigrationOperationDeserializationTests : IDisposable
    {
        // Fields.
        private readonly IMigrationsDbContext dbContext;
        private readonly IServiceScope scope;

        // Constructor.
        public DbMigrationOperationDeserializationTests(IntegrationFixture fixture)
        {
            ArgumentNullException.ThrowIfNull(fixture);

            scope = fixture.ServiceProvider.CreateScope();
            dbContext = scope.ServiceProvider.GetRequiredService<IMigrationsDbContext>();
        }

        // Dispose.
        public void Dispose()
        {
            scope.Dispose();
            GC.SuppressFinalize(this);
        }

        // Tests.
        // "afdb63c9-791b-41f8-8216-556e233df0de" - 0.25.0
        [Fact]
        public void DeserializeOperationWrittenBy0250()
        {
            // Setup.
            const string sourceDocument =
                """
                {
                    "_id" : ObjectId("62c0f7e5a0b1c2d3e4f50002"),
                    "_s" : "afdb63c9-791b-41f8-8216-556e233df0de",
                    "_t" : "DbMigrationOperation",
                    "DbContextName" : "MigrationsDbContext",
                    "CompletedDateTime" : ISODate("2026-09-01T10:05:00.000+0000"),
                    "CurrentStatus" : "Completed",
                    "IsDryRun" : true,
                    "IsStopAtFirstErrorEnabled" : true,
                    "Logs" : [
                        {
                            "_s" : "d2b49514-464e-4b28-8b38-ad2d0cc69d3e",
                            "_t" : "DocumentMigrationLog",
                            "State" : "Succeded",
                            "CreationDateTime" : ISODate("2026-09-01T10:01:00.000+0000"),
                            "CollectionName" : "notes",
                            "Errors" : [
                                {
                                    "_s" : "15b8f6c8-8e94-4849-a3ce-4f0eb2cbb556",
                                    "DocumentId" : "62c0f7e5a0b1c2d3e4f50003",
                                    "Message" : "Unknown schema id"
                                }
                            ],
                            "TotErrorDocs" : NumberLong(1),
                            "TotMigratedDocs" : NumberLong(42)
                        }
                    ],
                    "TaskId" : "1f77c0ea"
                }
                """;

            // Action.
            var operation = DeserializeOperation(sourceDocument);

            // Assert.
            Assert.Equal("62c0f7e5a0b1c2d3e4f50002", operation.Id);
            Assert.Equal(new DateTimeOffset(2026, 09, 01, 10, 05, 00, TimeSpan.Zero), operation.CompletedDateTime);
            Assert.Equal(DbMigrationOperation.Status.Completed, operation.CurrentStatus);
            Assert.Equal("MigrationsDbContext", operation.DbContextName);
            Assert.True(operation.IsDryRun);
            Assert.True(operation.IsStopAtFirstErrorEnabled);
            Assert.Equal("1f77c0ea", operation.TaskId);

            var documentLog = Assert.IsType<DocumentMigrationLog>(Assert.Single(operation.Logs));
            Assert.Equal("notes", documentLog.CollectionName);
            Assert.Equal(new DateTimeOffset(2026, 09, 01, 10, 01, 00, TimeSpan.Zero), documentLog.CreationDateTime);
            Assert.Equal(MigrationLogBase.ExecutionState.Succeded, documentLog.State);
            Assert.Equal(1, documentLog.TotErrorDocs);
            Assert.Equal(42, documentLog.TotMigratedDocs);
            var error = Assert.Single(documentLog.Errors);
            Assert.Equal("62c0f7e5a0b1c2d3e4f50003", error.DocumentId);
            Assert.Equal("Unknown schema id", error.Message);
        }

        // "afdb63c9-791b-41f8-8216-556e233df0de" - MongODM 0.24, schema id on the deprecated element
        [Fact]
        public void DeserializeOperationWrittenByMongodm024()
        {
            // Setup.
            const string sourceDocument =
                """
                {
                    "_id" : ObjectId("62c0f7e5a0b1c2d3e4f50001"),
                    "_m" : "afdb63c9-791b-41f8-8216-556e233df0de",
                    "_t" : "DbMigrationOperation",
                    "CreationDateTime" : ISODate("2024-06-01T10:00:00.000+0000"),
                    "DbContextName" : "MigrationsDbContext",
                    "CompletedDateTime" : ISODate("2024-06-01T10:05:00.000+0000"),
                    "CurrentStatus" : "Completed",
                    "Logs" : [
                        {
                            "_m" : "d2b49514-464e-4b28-8b38-ad2d0cc69d3e",
                            "_t" : "DocumentMigrationLog",
                            "State" : "Succeded",
                            "CreationDateTime" : ISODate("2024-06-01T10:01:00.000+0000"),
                            "CollectionName" : "notes",
                            "TotMigratedDocs" : NumberLong(42)
                        },
                        {
                            "_m" : "24d65670-a3c3-443c-977a-51112df04e2a",
                            "_t" : "IndexMigrationLog",
                            "State" : "Succeded",
                            "CreationDateTime" : ISODate("2024-06-01T10:02:00.000+0000"),
                            "Repository" : "notes"
                        }
                    ],
                    "TaskId" : "9d2ad0b6"
                }
                """;

            // Action.
            var operation = DeserializeOperation(sourceDocument);

            // Assert.
            Assert.Equal("62c0f7e5a0b1c2d3e4f50001", operation.Id);
            Assert.Equal(new DateTimeOffset(2024, 06, 01, 10, 05, 00, TimeSpan.Zero), operation.CompletedDateTime);
            Assert.Equal(DbMigrationOperation.Status.Completed, operation.CurrentStatus);
            Assert.Equal("MigrationsDbContext", operation.DbContextName);
            Assert.False(operation.IsDryRun);
            Assert.False(operation.IsStopAtFirstErrorEnabled);
            Assert.Equal("9d2ad0b6", operation.TaskId);

            var logs = operation.Logs.ToArray();
            Assert.Equal(2, logs.Length);

            //the migration errors didn't exist yet: the log reads with none of them
            var documentLog = Assert.IsType<DocumentMigrationLog>(logs[0]);
            Assert.Equal("notes", documentLog.CollectionName);
            Assert.Equal(new DateTimeOffset(2024, 06, 01, 10, 01, 00, TimeSpan.Zero), documentLog.CreationDateTime);
            Assert.Equal(MigrationLogBase.ExecutionState.Succeded, documentLog.State);
            Assert.Empty(documentLog.Errors);
            Assert.Equal(0, documentLog.TotErrorDocs);
            Assert.Equal(42, documentLog.TotMigratedDocs);

#pragma warning disable CS0618 // Type or member is obsolete
            var indexLog = Assert.IsType<IndexMigrationLog>(logs[1]);
            Assert.Equal("notes", indexLog.Repository);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.Equal(new DateTimeOffset(2024, 06, 01, 10, 02, 00, TimeSpan.Zero), indexLog.CreationDateTime);
            Assert.Equal(MigrationLogBase.ExecutionState.Succeded, indexLog.State);
        }

        // Helpers.
        private DbMigrationOperation DeserializeOperation(string sourceDocument)
        {
            using var documentReader = new JsonReader(sourceDocument);
            var modelMapSerializer = new ModelMapSerializer<DbMigrationOperation>(dbContext.Engine);

            using var asyncLocalContext = AsyncLocalContext.Instance.InitAsyncLocalContext();
            //read on the repository owning the operations, as the migration state reads do
            using var dbExecutionContext = new DbExecutionContextHandler(dbContext, dbContext.DbOperations);
            return modelMapSerializer.Deserialize(BsonDeserializationContext.CreateRoot(documentReader));
        }
    }
}
