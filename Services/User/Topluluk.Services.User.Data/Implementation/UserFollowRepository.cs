using DBHelper.Connection;
using DBHelper.Repository.Mongo;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver;
using System.Linq.Expressions;
using Topluluk.Services.User.Data.Interface;
using Topluluk.Services.User.Model.Entity;
using Topluluk.Shared.Dtos;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Topluluk.Services.User.Data.Implementation;

public class UserFollowRepository : MongoGenericRepository<UserFollow>, IUserFollowRepository
{
    private readonly DBHelper.Connection.IConnectionFactory _connectionFactory;

    public UserFollowRepository(DBHelper.Connection.IConnectionFactory connectionFactory) : base(connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    private IMongoDatabase GetConnection() => (MongoDB.Driver.IMongoDatabase)_connectionFactory.GetConnection;

    private string GetCollectionName() => string.Format("{0}Collection", typeof(UserFollow).Name);

    public async Task<List<string>?> GetFollowerIds(int skip, int take, Expression<Func<UserFollow, bool>> expression)
    {
        var database = GetConnection();
        var collectionName = GetCollectionName();
        var response = await database.GetCollection<UserFollow>(collectionName).Find(expression).Skip(skip).Limit(take).Project(f => f.SourceId).ToListAsync();
        return response;
    }

    public async Task<List<string>?> GetFollowingIds(int skip, int take, Expression<Func<UserFollow, bool>> expression)
    {
        var database = GetConnection();
        var collectionName = GetCollectionName();
        var response = await database.GetCollection<UserFollow>(collectionName).Find(expression).Skip(skip).Limit(take).Project(f => f.TargetId).ToListAsync();
        return response;
    }
}