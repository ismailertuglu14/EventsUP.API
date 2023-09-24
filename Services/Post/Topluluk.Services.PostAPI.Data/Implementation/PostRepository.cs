using System;
using System.Linq.Expressions;
using DBHelper.Connection;
using DBHelper.Repository.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Topluluk.Services.PostAPI.Data.Interface;
using Topluluk.Services.PostAPI.Model.Entity;

namespace Topluluk.Services.PostAPI.Data.Implementation
{
	public class PostRepository : MongoGenericRepository<Post>,  IPostRepository
	{

        private readonly IConnectionFactory _connectionFactory;

		public PostRepository(IConnectionFactory connectionFactory) : base(connectionFactory)
		{
            _connectionFactory = connectionFactory;
        }
        private new IMongoDatabase GetConnection() => (MongoDB.Driver.IMongoDatabase)_connectionFactory.GetConnection;

        private string GetCollectionName() => string.Format("{0}Collection", typeof(Post).Name);

        public async Task<bool> DeletePosts(string userId)
        {

            var database = GetConnection();
            var collectionName = GetCollectionName();

            var filter = Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(p => p.User.Id, userId),
                Builders<Post>.Filter.Eq(p => p.IsDeleted, false));

            var update = Builders<Post>.Update.Set(p => p.IsDeleted, true);

            var result = database.GetCollection<Post>(collectionName).UpdateMany(filter, update);
            return await Task.FromResult(result.ModifiedCount > 0);
        }

        /// <summary>
        /// Get posts descending by create date
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public async Task<List<Post>> GetPostsWithDescending(int skip, int take, Expression<Func<Post, bool>> expression)
        {
            var database = GetConnection();
            var collectionName = GetCollectionName();
            var filter = Builders<Post>.Filter.Where(expression);
            var sort = Builders<Post>.Sort.Descending(p => p.CreatedAt);

            var cursor = await database.GetCollection<Post>(collectionName)
                .Find(filter)
                .Sort(sort)
                .Skip(skip * take)
                .Limit(take)
                .ToCursorAsync();

            var documents = await cursor.ToListAsync();
            return documents;
        }
    }
}

