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
using Etherna.MongoDB.Driver;
using Etherna.Scrinium.Core.ExecContext.AsyncLocal;
using Etherna.Scrinium.IntegrationTests.Fixtures;
using Etherna.Scrinium.IntegrationTests.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Etherna.Scrinium.IntegrationTests
{
    /* SCR-284: the insert of a create and the implicit unit of work flush it triggers must
     * be one atomic unit, so a failure of either leaves nothing behind instead of orphaning
     * the inserted document. The seals collection carries a unique index on the artifact
     * fingerprint (CustomIdDbContext): writing a duplicate fingerprint fails deterministically,
     * standing in for the write conflict of the reported production scenario. */
    [Collection("Integration")]
    public class TransactionalCreateTests : IDisposable
    {
        // Fields.
        private readonly ICustomIdDbContext dbContext;
        private readonly IntegrationFixture fixture;
        private readonly IServiceScope serviceScope;

        // Constructor and dispose.
        /* Each test runs on its own DI scope, resolving fresh db context instances
         * like a production request or job would do. */
        public TransactionalCreateTests(IntegrationFixture fixture)
        {
            this.fixture = fixture;
            serviceScope = fixture.ServiceProvider.CreateScope();
            dbContext = serviceScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
        }

        public void Dispose()
        {
            serviceScope.Dispose();
            GC.SuppressFinalize(this);
        }

        // Tests.
        [Fact]
        public async Task CreateRollsBackTheInsertWhenTheImplicitFlushFails()
        {
            /* The reported scenario: a create whose insert succeeds, followed by the implicit
             * flush of another pending change that fails. Without the transaction the inserted
             * document survives as an orphan while the flush rolls back. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            await dbContext.Seals.BuildNewIndexesAsync();
            var occupyingSeal = new Seal(new Fingerprint("scr284-flush-occupied"));
            var otherSeal = new Seal(new Fingerprint("scr284-flush-other"));
            await dbContext.Seals.CreateAsync(occupyingSeal);
            await dbContext.Seals.CreateAsync(otherSeal);

            // Action.
            using var workScope = fixture.ServiceProvider.CreateScope();
            var workDbContext = workScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
            using var workContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();

            //pending change that will violate the unique index when the create flushes it
            var loadedOtherSeal = await workDbContext.Seals.FindOneAsync(otherSeal.Id);
            loadedOtherSeal.ArtifactFingerprint = new Fingerprint("scr284-flush-occupied");

            var createdSeal = new Seal(new Fingerprint("scr284-flush-created"));
            await Assert.ThrowsAnyAsync<MongoException>(() => workDbContext.Seals.CreateAsync(createdSeal));

            // Assert.
            //the aborted transaction rolled back the insert and undid the create: no orphan document, no id
            Assert.Null(createdSeal.Id);
            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
            var sealsCollection = readDbContext.Engine.Database.GetCollection<BsonDocument>("seals");
            Assert.False(await sealsCollection.Find(
                Builders<BsonDocument>.Filter.Eq("ArtifactFingerprint", "scr284-flush-created")).AnyAsync());
        }

        [Fact]
        public async Task CreateManyRollsBackAllInsertsWhenOneOfThemFails()
        {
            /* A batch create whose second insert violates the unique index: without the
             * transaction the first insert survives as an orphan while the batch fails. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            await dbContext.Seals.BuildNewIndexesAsync();

            // Action.
            var firstSeal = new Seal(new Fingerprint("scr284-batch-duplicate"));
            var duplicateSeal = new Seal(new Fingerprint("scr284-batch-duplicate"));
            await Assert.ThrowsAnyAsync<MongoException>(() =>
                dbContext.Seals.CreateAsync([firstSeal, duplicateSeal]));

            // Assert.
            //the aborted transaction rolled back the first insert too, and undid both creates
            Assert.Null(firstSeal.Id);
            Assert.Null(duplicateSeal.Id);
            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
            var sealsCollection = readDbContext.Engine.Database.GetCollection<BsonDocument>("seals");
            Assert.False(await sealsCollection.Find(
                Builders<BsonDocument>.Filter.Eq("ArtifactFingerprint", "scr284-batch-duplicate")).AnyAsync());
        }
    }
}
