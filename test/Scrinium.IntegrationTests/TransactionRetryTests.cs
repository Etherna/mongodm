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
    /* SCR-285: a transaction failing with the transient error label replays its block, and the
     * replay starts from the unit of work as the aborted attempt left it: the saves still
     * pending (see TransactionalSaveTests), the creates undone. A second session holding an
     * uncommitted write on the same document stands in for the concurrent transaction of the
     * reported scenario: the enlisted save fails with a write conflict, labeled transient by
     * the server. */
    [Collection("Integration")]
    public class TransactionRetryTests : IDisposable
    {
        // Fields.
        private readonly ITestDbContext dbContext;
        private readonly IntegrationFixture fixture;
        private readonly IServiceScope serviceScope;

        // Constructor and dispose.
        /* Each test runs on its own DI scope, resolving fresh db context instances
         * like a production request or job would do. */
        public TransactionRetryTests(IntegrationFixture fixture)
        {
            this.fixture = fixture;
            serviceScope = fixture.ServiceProvider.CreateScope();
            dbContext = serviceScope.ServiceProvider.GetRequiredService<ITestDbContext>();
        }

        public void Dispose()
        {
            serviceScope.Dispose();
            GC.SuppressFinalize(this);
        }

        // Tests.
        [Fact]
        public async Task AbortedTransactionUndoesTheCreatedModel()
        {
            /* A create rolled back by the abort leaves nothing behind in the scope either: the
             * assigned id returns to null, the instance leaves the identity map, and the same
             * instance creates anew afterwards, like a never persisted one. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var post = new Post("undo title", "undo content");

            // Action.
            string? rolledBackId = null;
            await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.ExecuteInTransactionAsync(async () =>
            {
                await dbContext.Posts.CreateAsync(post);
                rolledBackId = post.Id;
                throw new InvalidOperationException();
            }));

            // Assert.
            Assert.NotNull(rolledBackId);
            Assert.Null(post.Id);
            Assert.Null(dbContext.TryGetLoadedModel(dbContext.Posts, rolledBackId!));

            await dbContext.Posts.CreateAsync(post);
            Assert.NotNull(post.Id);

            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            using var readContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            Assert.Equal("undo title", (await readDbContext.Posts.FindOneAsync(post.Id)).Title);
        }

        [Fact]
        public async Task RetriedTransactionCreatesTheNewReferredModelsAnew()
        {
            /* The replay of a create must not reference the documents of the rolled back attempt:
             * the auto created referred models, undone with their ids, discover as new again and
             * insert again, so every reference of the committed documents resolves. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var post = new Post("retry post", "retry content");
            var blog = new Blog("retry blog");
            blog.AddPost(post);
            var bookmark = new Bookmark("retry bookmark", blog);

            // Action.
            var attempts = 0;
            await dbContext.ExecuteInTransactionAsync(async () =>
            {
                attempts++;
                await dbContext.Bookmarks.CreateAsync(bookmark);

                //a transient failure after the create, on its first attempt only
                if (attempts == 1)
                    throw NewTransientException();
            });

            // Assert.
            Assert.Equal(2, attempts);

            //the whole chain of new models persisted by the replay, with complete references at every level
            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            var bookmarksCollection = readDbContext.Engine.Database.GetCollection<BsonDocument>("bookmarks");
            var rawBookmark = await bookmarksCollection.Find(
                Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(bookmark.Id))).SingleAsync();
            Assert.Equal(ObjectId.Parse(blog.Id), rawBookmark["Blog"]["_id"].AsObjectId);

            var blogsCollection = readDbContext.Engine.Database.GetCollection<BsonDocument>("blogs");
            var rawBlog = await blogsCollection.Find(
                Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(blog.Id))).SingleAsync();
            Assert.Equal(ObjectId.Parse(post.Id), rawBlog["LastPost"]["_id"].AsObjectId);

            var postsCollection = readDbContext.Engine.Database.GetCollection<BsonDocument>("posts");
            Assert.True(await postsCollection.Find(
                Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(post.Id))).AnyAsync());
        }

        [Fact]
        public async Task TransientWriteConflictIsRetriedUntilTheTransactionCommits()
        {
            /* The reported scenario: a flush saving a document while a concurrent transaction
             * holds an uncommitted write on it fails with a write conflict, labeled transient by
             * the server. The retry replays the flush once the other transaction committed, and
             * the disjoint members of the two writers both survive: the save re-applies only the
             * members changed against the model snapshot. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var post = new Post("original title", "original content");
            await dbContext.Posts.CreateAsync(post);

            //a concurrent transaction on its own session, holding an uncommitted write on the document
            var rawPostsCollection = dbContext.Engine.Client.GetDatabase(fixture.TestDbName).GetCollection<BsonDocument>("posts");
            using var otherSession = await dbContext.Engine.Client.StartSessionAsync();
            otherSession.StartTransaction();
            await rawPostsCollection.UpdateOneAsync(
                otherSession,
                Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(post.Id)),
                Builders<BsonDocument>.Update.Set("Title", "concurrent title"));

            // Action.
            using var workScope = fixture.ServiceProvider.CreateScope();
            var workDbContext = workScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            using var workContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var loadedPost = await workDbContext.Posts.FindOneAsync(post.Id);
            var attempts = 0;
            await workDbContext.ExecuteInTransactionAsync(async () =>
            {
                attempts++;

                //the first attempt conflicts with the uncommitted write: the replay finds it committed
                if (attempts == 2)
                    await otherSession.CommitTransactionAsync();

                loadedPost.Content = "retried content";
                await workDbContext.SaveChangesAsync();
            });

            // Assert.
            Assert.Equal(2, attempts);
            Assert.DoesNotContain(loadedPost, workDbContext.ChangedModelsList);

            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            using var readContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var foundPost = await readDbContext.Posts.FindOneAsync(post.Id);
            Assert.Equal("concurrent title", foundPost.Title);
            Assert.Equal("retried content", foundPost.Content);
        }

        // Helpers.
        private static MongoException NewTransientException()
        {
            var exception = new MongoException("transient failure");
            exception.AddErrorLabel("TransientTransactionError");
            return exception;
        }
    }
}
