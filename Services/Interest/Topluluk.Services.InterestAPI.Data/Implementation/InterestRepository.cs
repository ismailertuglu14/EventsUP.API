using DBHelper.Connection;
using DBHelper.Repository.Mongo;
using MongoDB.Driver;
using Topluluk.Services.InterestAPI.Data.Interface;
using Topluluk.Services.InterestAPI.Model.Entity;

namespace Topluluk.Services.InterestAPI.Data.Implementation;

public class InterestRepository: MongoGenericRepository<Interest>, IInterestRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public InterestRepository(IConnectionFactory connectionFactory) : base(connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private IMongoDatabase GetConnection() => (MongoDB.Driver.IMongoDatabase)_connectionFactory.GetConnection;

    private string GetCollectionName() => $"{nameof(Interest)}Collection";



}