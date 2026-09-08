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
using Etherna.Scrinium.Core.ProxyModels;
using Etherna.Scrinium.IntegrationTests.Fixtures;
using Etherna.Scrinium.IntegrationTests.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Etherna.Scrinium.IntegrationTests
{
    [Collection("Integration")]
    public class SaveModelChangesTests : IDisposable
    {
        // Fields.
        private readonly ITestDbContext dbContext;
        private readonly IntegrationFixture fixture;
        private readonly IServiceScope serviceScope;

        // Constructor and dispose.
        /* Each test runs on its own DI scope, resolving fresh db context instances
         * like a production request or job would do. */
        public SaveModelChangesTests(IntegrationFixture fixture)
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
        public async Task SaveChangesUpdatesOnlyChangedMembersAndRefreshesModel()
        {
            /* Concurrent changes to disjoint members of the same document must all survive:
             * the save updates only the changed members, and refreshes the saved model with
             * the returned document state, including the concurrent changes. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var post = new Post("original title", "original content");
            await dbContext.Posts.CreateAsync(post);

            var loadedPost = await dbContext.Posts.FindOneAsync(post.Id);
            loadedPost.Content = "content from A";

            //concurrent update of a different member from another scope
            using (var externalScope = fixture.ServiceProvider.CreateScope())
            {
                var externalDbContext = externalScope.ServiceProvider.GetRequiredService<ITestDbContext>();
                var externalPost = await externalDbContext.Posts.FindOneAsync(post.Id);
                externalPost.Title = "title from B";
                await externalDbContext.SaveChangesAsync();
            }

            // Action.
            await dbContext.SaveChangesAsync();

            // Assert.
            //the saved model is refreshed with the concurrent change
            Assert.Equal("title from B", loadedPost.Title);
            Assert.Equal("content from A", loadedPost.Content);

            //both changes are merged on db
            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            using var readContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var foundPost = await readDbContext.Posts.FindOneAsync(post.Id);
            Assert.Equal("title from B", foundPost.Title);
            Assert.Equal("content from A", foundPost.Content);
        }

        [Fact]
        public async Task SaveChangesReplacesDocumentsNotOnActiveSchema()
        {
            /* Documents serialized with a not active schema can't receive member level
             * updates, or members of different schemas would mix into a broken document:
             * the save falls back to a whole document replace, migrating them. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var post = new Post("title", "content");
            await dbContext.Posts.CreateAsync(post);

            //simulate a document serialized with an old schema
            var postsCollection = dbContext.Engine.Database.GetCollection<BsonDocument>("posts");
            var postFilter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(post.Id));
            await postsCollection.UpdateOneAsync(postFilter,
                Builders<BsonDocument>.Update.Set("_s", "legacy-schema-id"));

            // Action.
            var loadedPost = await dbContext.Posts.FindOneAsync(post.Id);
            loadedPost.Content = "updated content";
            await dbContext.SaveChangesAsync();

            // Assert.
            //the document has been migrated to the active schema, with the change persisted
            var rawPost = await postsCollection.Find(postFilter).SingleAsync();
            Assert.NotEqual("legacy-schema-id", rawPost["_s"].AsString);
            Assert.Equal("updated content", rawPost["Content"].AsString);
            Assert.Equal("title", rawPost["Title"].AsString);
        }

        [Fact]
        public async Task SaveRefreshKeepsTheCreatedReferenceInstances()
        {
            /* SCR-281: a created model registers on the identity map, so the save refresh of a
             * model referencing it resolves the reference to the created instance itself,
             * instead of replacing it with a new summary of its document. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var blog = new Blog("blog title");
            await dbContext.Blogs.CreateAsync(blog);

            //load on a new scope, and create the post to add inside it
            using var saveScope = fixture.ServiceProvider.CreateScope();
            var saveDbContext = saveScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            using var saveContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();

            var loadedBlog = await saveDbContext.Blogs.FindOneAsync(blog.Id);
            var post = new Post("post title", "post content");
            await saveDbContext.Posts.CreateAsync(post);

            // Action.
            loadedBlog.AddPost(post);
            await saveDbContext.SaveChangesAsync();

            // Assert.
            //the refreshed reference members are the created instance, as it is
            Assert.Same(post, loadedBlog.LastPost);
            Assert.Same(post, loadedBlog.Posts.Single());
            Assert.Same(post, saveDbContext.TryGetLoadedModel(saveDbContext.Posts, post.Id));
        }

        [Fact]
        public async Task SaveRefreshKeepsTheLoadedReferenceInstances()
        {
            /* SCR-280: the save refreshes the saved model from the returned document, whose
             * references resolve through the identity map like any deserialization: a
             * referenced instance loaded in the scope stays the value of its member, never
             * replaced by a new summary of the same document. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var firstPost = new Post("first title", "first content");
            var secondPost = new Post("second title", "second content");
            await dbContext.Posts.CreateAsync(firstPost);
            await dbContext.Posts.CreateAsync(secondPost);
            var blog = new Blog("blog title");
            blog.AddPost(firstPost);
            await dbContext.Blogs.CreateAsync(blog);

            //load on a new scope: the referenced post is a summary, the post to add a full instance
            using var saveScope = fixture.ServiceProvider.CreateScope();
            var saveDbContext = saveScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            using var saveContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();

            var loadedBlog = await saveDbContext.Blogs.FindOneAsync(blog.Id);
            var firstPostSummary = loadedBlog.LastPost!;
            var secondFullPost = await saveDbContext.Posts.FindOneAsync(secondPost.Id);
            Assert.True(((IReferenceable)firstPostSummary).IsSummary);
            Assert.False(((IReferenceable)secondFullPost).IsSummary);

            // Action.
            loadedBlog.AddPost(secondFullPost);
            await saveDbContext.SaveChangesAsync();

            // Assert.
            //the refreshed reference members are the instances loaded in the scope, as they are
            Assert.Same(secondFullPost, loadedBlog.LastPost);
            var refreshedPosts = loadedBlog.Posts.ToArray();
            Assert.Equal(2, refreshedPosts.Length);
            Assert.Same(firstPostSummary, refreshedPosts[0]);
            Assert.Same(secondFullPost, refreshedPosts[1]);
            Assert.True(((IReferenceable)firstPostSummary).IsSummary);
            Assert.False(((IReferenceable)secondFullPost).IsSummary);
            Assert.Same(firstPostSummary, saveDbContext.TryGetLoadedModel(saveDbContext.Posts, firstPost.Id));
            Assert.Same(secondFullPost, saveDbContext.TryGetLoadedModel(saveDbContext.Posts, secondPost.Id));
        }

        [Fact]
        public async Task SaveRefreshOfASummaryKeepsTheLoadedReferenceInstances()
        {
            /* SCR-280: a saved summary upgrades from the returned document, whose references
             * resolve through the identity map too: the nested reference it carries stays the
             * instance loaded in the scope, instead of a new summary of the same document. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var post = new Post("post title", "post content");
            await dbContext.Posts.CreateAsync(post);
            var blog = new Blog("blog title");
            blog.AddPost(post);
            await dbContext.Blogs.CreateAsync(blog);
            var bookmark = new Bookmark("label", blog);
            await dbContext.Bookmarks.CreateAsync(bookmark);

            //load on a new scope: the blog summary nests the post summary, upgraded by the preload
            using var saveScope = fixture.ServiceProvider.CreateScope();
            var saveDbContext = saveScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            using var saveContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();

            var loadedBookmark = await saveDbContext.Bookmarks.FindOneAsync(bookmark.Id);
            var blogSummary = loadedBookmark.Blog;
            var nestedPost = blogSummary.LastPost!;
            await saveDbContext.LoadValuesAsync(nestedPost, p => p.Content);
            Assert.True(((IReferenceable)blogSummary).IsSummary);
            Assert.False(((IReferenceable)nestedPost).IsSummary);

            // Action.
            blogSummary.Title = "updated title";
            await saveDbContext.SaveChangesAsync();

            // Assert.
            //the summary upgraded, keeping the nested reference instance loaded in the scope
            Assert.False(((IReferenceable)blogSummary).IsSummary);
            Assert.Same(nestedPost, blogSummary.LastPost);
            Assert.Same(nestedPost, blogSummary.Posts.Single());
            Assert.False(((IReferenceable)nestedPost).IsSummary);
            Assert.Same(nestedPost, saveDbContext.TryGetLoadedModel(saveDbContext.Posts, post.Id));
        }

        [Fact]
        public async Task SavingAModelHostingSummariesDoesNotLoadThem()
        {
            /* SCR-279: the save diffs the model reserializing its members, the summaries of
             * its references included. Writing a summary reads its extra elements bag, which
             * is never loaded data: the write must not load the origin documents, or every
             * save would cost one query per hosted summary. */

            // Setup.
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var firstPost = new Post("first title", "content");
            var secondPost = new Post("second title", "content");
            await dbContext.Posts.CreateAsync(firstPost);
            await dbContext.Posts.CreateAsync(secondPost);
            var blog = new Blog("blog title");
            blog.AddPost(firstPost);
            blog.AddPost(secondPost);
            await dbContext.Blogs.CreateAsync(blog);

            //load on a new scope: the referenced posts are summaries
            using var saveScope = fixture.ServiceProvider.CreateScope();
            var saveDbContext = saveScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            using var saveContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();

            var loadedBlog = await saveDbContext.Blogs.FindOneAsync(blog.Id);
            var loadedPosts = loadedBlog.Posts.ToArray();
            Assert.All(loadedPosts, p => Assert.True(((IReferenceable)p).IsSummary));

            // Action.
            loadedBlog.Title = "updated title";
            var findCommandsBefore = await GetServerFindCommandCountAsync();
            await saveDbContext.SaveChangesAsync();
            var findCommandsAfter = await GetServerFindCommandCountAsync();

            // Assert.
            //the summaries were written from their denormalized members, without a read
            Assert.Equal(0, findCommandsAfter - findCommandsBefore);
            Assert.All(loadedPosts, p => Assert.True(((IReferenceable)p).IsSummary));

            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            using var readContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var foundBlog = await readDbContext.Blogs.FindOneAsync(blog.Id);
            Assert.Equal("updated title", foundBlog.Title);
        }

        [Fact]
        public async Task SavingChangedSummaryUpdatesOnlyItsChangesAndUpgradesIt()
        {
            /* Saving a changed summary reference updates only its changed members, without
             * serializing (and lazy loading) the whole document. The refresh with the
             * returned document state upgrades the summary to a full model. */

            // Setup.
            //create on a setup scope: the test scope loads the documents fresh
            using var setupScope = fixture.ServiceProvider.CreateScope();
            var setupDbContext = setupScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var post = new Post("post title", "post content");
            await setupDbContext.Posts.CreateAsync(post);
            var blog = new Blog("blog title");
            blog.AddPost(post);
            await setupDbContext.Blogs.CreateAsync(blog);

            using var workContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var loadedBlog = await dbContext.Blogs.FindOneAsync(blog.Id);
            var referencedPost = loadedBlog.LastPost!;
            Assert.True(((IReferenceable)referencedPost).IsSummary);

            // Action.
            referencedPost.Title = "updated title";
            await dbContext.SaveChangesAsync();

            // Assert.
            //the refresh upgraded the summary with the whole document state
            Assert.False(((IReferenceable)referencedPost).IsSummary);
            Assert.Equal("post content", referencedPost.Content);

            //on db, the change is persisted and the not loaded members are intact
            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ITestDbContext>();
            using var readContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var foundPost = await readDbContext.Posts.FindOneAsync(post.Id);
            Assert.Equal("updated title", foundPost.Title);
            Assert.Equal("post content", foundPost.Content);
        }

        [Fact]
        public async Task SaveWithDocumentReplaceOptionKeepsWholeDocumentSemantics()
        {
            /* Repositories opting into SaveWithDocumentReplace persist changed models
             * replacing the whole document: concurrent changes to other members are
             * overwritten by the saved model state, last writer wins on the document. */

            // Setup.
            using var scope = fixture.ServiceProvider.CreateScope();
            var secondDbContext = scope.ServiceProvider.GetRequiredService<ISecondDbContext>();
            using var contextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();

            var note = new Note("original text") { Tag = "original tag" };
            await secondDbContext.Notes.CreateAsync(note);

            var loadedNote = await secondDbContext.Notes.FindOneAsync(note.Id);
            loadedNote.Text = "text from A";

            //concurrent update of a different member from another scope
            using (var externalScope = fixture.ServiceProvider.CreateScope())
            {
                var externalDbContext = externalScope.ServiceProvider.GetRequiredService<ISecondDbContext>();
                var externalNote = await externalDbContext.Notes.FindOneAsync(note.Id);
                externalNote.Tag = "tag from B";
                await externalDbContext.SaveChangesAsync();
            }

            // Action.
            await secondDbContext.SaveChangesAsync();

            // Assert.
            //whole document semantics: the concurrent member change is overwritten
            using var readScope = fixture.ServiceProvider.CreateScope();
            var readDbContext = readScope.ServiceProvider.GetRequiredService<ISecondDbContext>();
            using var readContextHandler = AsyncLocalContext.Instance.InitAsyncLocalContext();
            var foundNote = await readDbContext.Notes.FindOneAsync(note.Id);
            Assert.Equal("text from A", foundNote.Text);
            Assert.Equal("original tag", foundNote.Tag);
        }

        // Helpers.
        private async Task<long> GetServerFindCommandCountAsync()
        {
            var serverStatus = await dbContext.Engine.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("serverStatus", 1));
            return serverStatus["metrics"]["commands"]["find"]["total"].ToInt64();
        }
    }
}
