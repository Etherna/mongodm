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

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.IO;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Driver;
using Etherna.Scrinium.Core.Conventions;
using Etherna.Scrinium.Core.Domain.Models;
using Etherna.Scrinium.Core.Domain.Models.DbMigrationOpAgg;
using Etherna.Scrinium.Core.ExecContext.AsyncLocal;
using Etherna.Scrinium.Core.Models;
using Etherna.Scrinium.Core.Options;
using Etherna.Scrinium.Core.ProxyModels;
using Etherna.Scrinium.Core.Repositories;
using Etherna.Scrinium.Core.Serialization.Mapping;
using Etherna.Scrinium.Core.Serialization.Modifiers;
using Etherna.Scrinium.Core.Serialization.Serializers;
using Etherna.Scrinium.Core.Utility;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Etherna.Scrinium.Core.Domain.ModelMaps
{
    public class DbMigrationOperationDeserializationTest : IDisposable
    {
        // Internal classes.
        public class DeserializationTestElement(string sourceDocument, DbMigrationOperation expectedModel)
        {
            public DbMigrationOperation ExpectedModel { get; } = expectedModel;
            public string SourceDocument { get; } = sourceDocument;
        }

        // Fields.
        private readonly FakeDbContext dbContext = new();
        private readonly IDbContextEngine engine;

        // Constructor.
        /* Build a real engine over a real map registry: the documents deserialize through the
         * internal model maps registered by the engine initialization, as they do in an
         * application reading its own operations collection. */
        public DbMigrationOperationDeserializationTest()
        {
            var dependenciesMock = new Mock<IDbDependencies>();
            dependenciesMock.Setup(d => d.BsonSerializerRegistry)
                .Returns(new BsonSerializerRegistry());
            dependenciesMock.Setup(d => d.DbMaintainer)
                .Returns(new Mock<IDbMaintainer>().Object);
            dependenciesMock.Setup(d => d.DbMigrationManager)
                .Returns(new Mock<IDbMigrationManager>().Object);
            dependenciesMock.Setup(d => d.DiscriminatorRegistry)
                .Returns(new DiscriminatorRegistry());
            dependenciesMock.Setup(d => d.ExecutionContext)
                .Returns(AsyncLocalContext.Instance);
            dependenciesMock.Setup(d => d.MapRegistry)
                .Returns(new MapRegistry());
            dependenciesMock.Setup(d => d.ProxyGenerator)
                .Returns(new ProxyGenerator(AsyncLocalContext.Instance));
            dependenciesMock.Setup(d => d.RepositoryRegistry)
                .Returns(new RepositoryRegistry());
            dependenciesMock.Setup(d => d.SerializerModifierAccessor)
                .Returns(new SerializerModifierAccessor(AsyncLocalContext.Instance));

            /* Register the static integration with the driver, like the service collection
             * extension of the library does at startup: the driver resolves the discriminator
             * convention and the serialization context from its own statics while deserializing
             * the documents nested in an operation. */
            try
            {
                BsonSerializer.RegisterDiscriminatorConvention(typeof(object),
                    new HierarchicalProxyTolerantDiscriminatorConvention("_t", AsyncLocalContext.Instance));
            }
            catch (BsonSerializationException)
            { }
            BsonSerializer.SetSerializationContextAccessor(
                new SerializationContextAccessor(AsyncLocalContext.Instance));

            var mongoClientMock = new Mock<IMongoClient>();
            mongoClientMock.Setup(c => c.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                .Returns(new Mock<IMongoDatabase>().Object);

            engine = dbContext.BuildEngine(
                dependenciesMock.Object,
                mongoClientMock.Object,
                new DbContextOptions());
            dbContext.AttachToEngine(engine, [], dependenciesMock.Object.RepositoryRegistry);

            //deserialize into plain instances: the test reads the mapped members, not a repository operation
            engine.ProxyGenerator.DisableCreationWithProxyTypes = true;
        }

        // Dispose.
        public void Dispose()
        {
            (engine as IDisposable)?.Dispose();
            GC.SuppressFinalize(this);
        }

        // Data.
        public static IEnumerable<object[]> DbMigrationOperationDeserializationTests
        {
            get
            {
                var tests = new List<DeserializationTestElement>();

                // "afdb63c9-791b-41f8-8216-556e233df0de" - 0.25.0
                {
                    var sourceDocument =
                        """
                        {
                            "_id" : ObjectId("62c0f7e5a0b1c2d3e4f50002"),
                            "_s" : "afdb63c9-791b-41f8-8216-556e233df0de",
                            "_t" : "DbMigrationOperation",
                            "DbContextName" : "FakeDbContext",
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
                                    "CollectionName" : "fakeModels",
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

                    var expectedOperationMock = new Mock<DbMigrationOperation>();
                    expectedOperationMock.Setup(op => op.Id).Returns("62c0f7e5a0b1c2d3e4f50002");
                    expectedOperationMock.Setup(op => op.CompletedDateTime).Returns(
                        new DateTimeOffset(2026, 09, 01, 10, 05, 00, TimeSpan.Zero));
                    expectedOperationMock.Setup(op => op.CurrentStatus).Returns(DbMigrationOperation.Status.Completed);
                    expectedOperationMock.Setup(op => op.DbContextName).Returns("FakeDbContext");
                    expectedOperationMock.Setup(op => op.IsDryRun).Returns(true);
                    expectedOperationMock.Setup(op => op.IsStopAtFirstErrorEnabled).Returns(true);
                    expectedOperationMock.Setup(op => op.TaskId).Returns("1f77c0ea");
                    {
                        var documentLogMock = new Mock<DocumentMigrationLog>();
                        documentLogMock.Setup(l => l.CollectionName).Returns("fakeModels");
                        documentLogMock.Setup(l => l.CreationDateTime).Returns(
                            new DateTimeOffset(2026, 09, 01, 10, 01, 00, TimeSpan.Zero));
                        documentLogMock.Setup(l => l.Errors).Returns(
                            [new DocumentMigrationError("62c0f7e5a0b1c2d3e4f50003", "Unknown schema id")]);
                        documentLogMock.Setup(l => l.State).Returns(MigrationLogBase.ExecutionState.Succeded);
                        documentLogMock.Setup(l => l.TotErrorDocs).Returns(1);
                        documentLogMock.Setup(l => l.TotMigratedDocs).Returns(42);

                        expectedOperationMock.Setup(op => op.Logs).Returns([documentLogMock.Object]);
                    }

                    tests.Add(new DeserializationTestElement(sourceDocument, expectedOperationMock.Object));
                }

                // "afdb63c9-791b-41f8-8216-556e233df0de" - MongODM 0.24, schema id on the deprecated element
                {
                    var sourceDocument =
                        """
                        {
                            "_id" : ObjectId("62c0f7e5a0b1c2d3e4f50001"),
                            "_m" : "afdb63c9-791b-41f8-8216-556e233df0de",
                            "_t" : "DbMigrationOperation",
                            "CreationDateTime" : ISODate("2024-06-01T10:00:00.000+0000"),
                            "DbContextName" : "FakeDbContext",
                            "CompletedDateTime" : ISODate("2024-06-01T10:05:00.000+0000"),
                            "CurrentStatus" : "Completed",
                            "Logs" : [
                                {
                                    "_m" : "d2b49514-464e-4b28-8b38-ad2d0cc69d3e",
                                    "_t" : "DocumentMigrationLog",
                                    "State" : "Succeded",
                                    "CreationDateTime" : ISODate("2024-06-01T10:01:00.000+0000"),
                                    "CollectionName" : "fakeModels",
                                    "TotMigratedDocs" : NumberLong(42)
                                },
                                {
                                    "_m" : "24d65670-a3c3-443c-977a-51112df04e2a",
                                    "_t" : "IndexMigrationLog",
                                    "State" : "Succeded",
                                    "CreationDateTime" : ISODate("2024-06-01T10:02:00.000+0000"),
                                    "Repository" : "fakeModels"
                                }
                            ],
                            "TaskId" : "9d2ad0b6"
                        }
                        """;

                    var expectedOperationMock = new Mock<DbMigrationOperation>();
                    expectedOperationMock.Setup(op => op.Id).Returns("62c0f7e5a0b1c2d3e4f50001");
                    expectedOperationMock.Setup(op => op.CompletedDateTime).Returns(
                        new DateTimeOffset(2024, 06, 01, 10, 05, 00, TimeSpan.Zero));
                    expectedOperationMock.Setup(op => op.CurrentStatus).Returns(DbMigrationOperation.Status.Completed);
                    expectedOperationMock.Setup(op => op.DbContextName).Returns("FakeDbContext");
                    expectedOperationMock.Setup(op => op.IsDryRun).Returns(false);
                    expectedOperationMock.Setup(op => op.IsStopAtFirstErrorEnabled).Returns(false);
                    expectedOperationMock.Setup(op => op.TaskId).Returns("9d2ad0b6");
                    {
                        //the migration errors didn't exist yet: the log reads with none of them
                        var documentLogMock = new Mock<DocumentMigrationLog>();
                        documentLogMock.Setup(l => l.CollectionName).Returns("fakeModels");
                        documentLogMock.Setup(l => l.CreationDateTime).Returns(
                            new DateTimeOffset(2024, 06, 01, 10, 01, 00, TimeSpan.Zero));
                        documentLogMock.Setup(l => l.Errors).Returns([]);
                        documentLogMock.Setup(l => l.State).Returns(MigrationLogBase.ExecutionState.Succeded);
                        documentLogMock.Setup(l => l.TotErrorDocs).Returns(0);
                        documentLogMock.Setup(l => l.TotMigratedDocs).Returns(42);

#pragma warning disable CS0618 // Type or member is obsolete
                        var indexLogMock = new Mock<IndexMigrationLog>();
                        indexLogMock.Setup(l => l.CreationDateTime).Returns(
                            new DateTimeOffset(2024, 06, 01, 10, 02, 00, TimeSpan.Zero));
                        indexLogMock.Setup(l => l.Repository).Returns("fakeModels");
                        indexLogMock.Setup(l => l.State).Returns(MigrationLogBase.ExecutionState.Succeded);
#pragma warning restore CS0618 // Type or member is obsolete

                        expectedOperationMock.Setup(op => op.Logs).Returns(
                            [documentLogMock.Object, indexLogMock.Object]);
                    }

                    tests.Add(new DeserializationTestElement(sourceDocument, expectedOperationMock.Object));
                }

                return tests.Select(t => new object[] { t });
            }
        }

        // Tests.
        [Theory, MemberData(nameof(DbMigrationOperationDeserializationTests))]
        public void DbMigrationOperationDeserialization(DeserializationTestElement testElement)
        {
            ArgumentNullException.ThrowIfNull(testElement);

            // Setup.
            using var documentReader = new JsonReader(testElement.SourceDocument);
            var modelMapSerializer = new ModelMapSerializer<DbMigrationOperation>(engine);
            var deserializationContext = BsonDeserializationContext.CreateRoot(documentReader);

            // Action.
            using var dbExecutionContext = new DbExecutionContextHandler(engine); //run into a db execution context
            var result = modelMapSerializer.Deserialize(deserializationContext);

            // Assert.
            Assert.Equal(testElement.ExpectedModel.Id, result.Id);
            Assert.Equal(testElement.ExpectedModel.CompletedDateTime, result.CompletedDateTime);
            Assert.Equal(testElement.ExpectedModel.CurrentStatus, result.CurrentStatus);
            Assert.Equal(testElement.ExpectedModel.DbContextName, result.DbContextName);
            Assert.Equal(testElement.ExpectedModel.IsDryRun, result.IsDryRun);
            Assert.Equal(testElement.ExpectedModel.IsStopAtFirstErrorEnabled, result.IsStopAtFirstErrorEnabled);
            Assert.Equal(testElement.ExpectedModel.TaskId, result.TaskId);

            var expectedLogs = testElement.ExpectedModel.Logs.ToArray();
            var resultLogs = result.Logs.ToArray();
            Assert.Equal(expectedLogs.Length, resultLogs.Length);
            foreach (var (expectedLog, resultLog) in expectedLogs.Zip(resultLogs))
            {
                Assert.Equal(expectedLog.CreationDateTime, resultLog.CreationDateTime);
                Assert.Equal(expectedLog.State, resultLog.State);

                switch (expectedLog)
                {
                    case DocumentMigrationLog expectedDocumentLog:
                        var resultDocumentLog = Assert.IsType<DocumentMigrationLog>(resultLog);
                        Assert.Equal(expectedDocumentLog.CollectionName, resultDocumentLog.CollectionName);
                        Assert.Equal(expectedDocumentLog.TotErrorDocs, resultDocumentLog.TotErrorDocs);
                        Assert.Equal(expectedDocumentLog.TotMigratedDocs, resultDocumentLog.TotMigratedDocs);
                        Assert.Equal(
                            expectedDocumentLog.Errors.Select(e => (e.DocumentId, e.Message)),
                            resultDocumentLog.Errors.Select(e => (e.DocumentId, e.Message)));
                        break;

#pragma warning disable CS0618 // Type or member is obsolete
                    case IndexMigrationLog expectedIndexLog:
                        var resultIndexLog = Assert.IsType<IndexMigrationLog>(resultLog);
                        Assert.Equal(expectedIndexLog.Repository, resultIndexLog.Repository);
                        break;
#pragma warning restore CS0618 // Type or member is obsolete

                    default:
                        Assert.Fail($"Unexpected log type {expectedLog.GetType().Name}");
                        break;
                }
            }
        }
    }
}
