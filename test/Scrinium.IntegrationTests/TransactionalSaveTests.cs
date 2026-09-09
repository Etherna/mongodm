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
    /* SCR-286: the change tracking of a save enlisted in a transaction follows the commit.
     * Refreshing the saved model and clearing its change candidate inside the transaction
     * leaves it looking saved after an abort, with its write rolled back: the next flush
     * diffs nothing and the change is lost silently. The seals collection carries a unique
     * index on the artifact fingerprint (CustomIdDbContext): writing an occupied fingerprint
     * fails deterministically, standing in for the write conflict of the reported scenario. */
    [Collection("Integration")]
    public class TransactionalSaveTests : IDisposable
    {
        // Fields.
        private readonly ICustomIdDbContext dbContext;
        private readonly IntegrationFixture fixture;
        private readonly IServiceScope serviceScope;

        // Constructor and dispose.
        /* Each test runs on its own DI scope, resolving fresh db context instances
         * like a production request or job would do. */
        public TransactionalSaveTests(IntegrationFixture fixture)
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
        public async Task AbortedTransactionKeepsTheReplacedModelPending()
        {
            /* The whole document replace refreshes the tracking like the member level save:
             * rolled back with its transaction, the replaced model stays pending too. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var seal = new Seal(new Fingerprint("scr286-replace-original"));
            await dbContext.Seals.CreateAsync(seal);

            // Action.
            using var workScope = fixture.ServiceProvider.CreateScope();
            var workDbContext = workScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
            using var workContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var loadedSeal = await workDbContext.Seals.FindOneAsync(seal.Id);
            await Assert.ThrowsAsync<InvalidOperationException>(() => workDbContext.ExecuteInTransactionAsync(async () =>
            {
                loadedSeal.ArtifactFingerprint = new Fingerprint("scr286-replace-updated");
                await workDbContext.Seals.ReplaceAsync(loadedSeal);
                throw new InvalidOperationException();
            }));

            // Assert.
            Assert.Contains(loadedSeal, workDbContext.ChangedModelsList);
            await workDbContext.SaveChangesAsync();

            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
            using var readContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            Assert.Equal(
                new Fingerprint("scr286-replace-updated"),
                (await readDbContext.Seals.FindOneAsync(seal.Id)).ArtifactFingerprint);
        }

        [Fact]
        public async Task AbortedTransactionKeepsTheSavedModelPending()
        {
            /* An explicit transaction aborted after a flush: the flushed change stays pending
             * on the db context, and the next flush persists it. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var seal = new Seal(new Fingerprint("scr286-abort-original"));
            await dbContext.Seals.CreateAsync(seal);

            // Action.
            using var workScope = fixture.ServiceProvider.CreateScope();
            var workDbContext = workScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
            using var workContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var loadedSeal = await workDbContext.Seals.FindOneAsync(seal.Id);
            await Assert.ThrowsAsync<InvalidOperationException>(() => workDbContext.ExecuteInTransactionAsync(async () =>
            {
                loadedSeal.ArtifactFingerprint = new Fingerprint("scr286-abort-updated");
                await workDbContext.SaveChangesAsync();
                throw new InvalidOperationException();
            }));

            // Assert.
            //the rolled back save left the model dirty: the next flush persists its change
            Assert.Contains(loadedSeal, workDbContext.ChangedModelsList);
            await workDbContext.SaveChangesAsync();

            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
            using var readContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            Assert.Equal(
                new Fingerprint("scr286-abort-updated"),
                (await readDbContext.Seals.FindOneAsync(seal.Id)).ArtifactFingerprint);
        }

        [Fact]
        public async Task CommittedTransactionClearsTheSavedModelAtTheCommit()
        {
            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var seal = new Seal(new Fingerprint("scr286-commit-original"));
            await dbContext.Seals.CreateAsync(seal);

            // Action.
            using var workScope = fixture.ServiceProvider.CreateScope();
            var workDbContext = workScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
            using var workContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var loadedSeal = await workDbContext.Seals.FindOneAsync(seal.Id);
            await workDbContext.ExecuteInTransactionAsync(async () =>
            {
                loadedSeal.ArtifactFingerprint = new Fingerprint("scr286-commit-updated");
                await workDbContext.SaveChangesAsync();

                //the flushed model stays a change candidate until the commit
                Assert.Contains(loadedSeal, workDbContext.ChangedModelsList);
            });

            // Assert.
            Assert.DoesNotContain(loadedSeal, workDbContext.ChangedModelsList);

            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
            using var readContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            Assert.Equal(
                new Fingerprint("scr286-commit-updated"),
                (await readDbContext.Seals.FindOneAsync(seal.Id)).ArtifactFingerprint);
        }

        [Fact]
        public async Task FailedImplicitFlushKeepsTheEarlierSavedModelsPending()
        {
            /* The reported scenario: a flush of two changed models whose second save fails.
             * The implicit transaction rolls the first save back too: the first model stays
             * pending, and the next flush persists it. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            await dbContext.Seals.BuildNewIndexesAsync();
            var occupyingSeal = new Seal(new Fingerprint("scr286-flush-occupied"));
            var firstSeal = new Seal(new Fingerprint("scr286-flush-first"));
            var secondSeal = new Seal(new Fingerprint("scr286-flush-second"));
            await dbContext.Seals.CreateAsync(occupyingSeal);
            await dbContext.Seals.CreateAsync(firstSeal);
            await dbContext.Seals.CreateAsync(secondSeal);

            // Action.
            using var workScope = fixture.ServiceProvider.CreateScope();
            var workDbContext = workScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
            using var workContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var loadedFirstSeal = await workDbContext.Seals.FindOneAsync(firstSeal.Id);
            var loadedSecondSeal = await workDbContext.Seals.FindOneAsync(secondSeal.Id);

            //the first model saves, then the second violates the unique index and aborts the flush
            loadedFirstSeal.ArtifactFingerprint = new Fingerprint("scr286-flush-first-updated");
            loadedSecondSeal.ArtifactFingerprint = new Fingerprint("scr286-flush-occupied");
            await Assert.ThrowsAnyAsync<MongoException>(() => workDbContext.SaveChangesAsync());

            // Assert.
            //the first model is still dirty: with the second reverted, the next flush persists it
            Assert.Contains(loadedFirstSeal, workDbContext.ChangedModelsList);
            loadedSecondSeal.ArtifactFingerprint = new Fingerprint("scr286-flush-second");
            await workDbContext.SaveChangesAsync();

            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ICustomIdDbContext>();
            using var readContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            Assert.Equal(
                new Fingerprint("scr286-flush-first-updated"),
                (await readDbContext.Seals.FindOneAsync(firstSeal.Id)).ArtifactFingerprint);
            Assert.Equal(
                new Fingerprint("scr286-flush-second"),
                (await readDbContext.Seals.FindOneAsync(secondSeal.Id)).ArtifactFingerprint);
        }
    }
}
