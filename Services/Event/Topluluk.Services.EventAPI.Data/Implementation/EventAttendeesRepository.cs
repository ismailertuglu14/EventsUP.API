using DBHelper.Connection;
using DBHelper.Repository.Mongo;
using MongoDB.Driver;
using Topluluk.Services.EventAPI.Data.Interface;
using Topluluk.Services.EventAPI.Model.Entity;

namespace Topluluk.Services.EventAPI.Data.Implementation;

public class EventAttendeesRepository : MongoGenericRepository<EventAttendee>, IEventAttendeesRepository
{
    private readonly DBHelper.Connection.IConnectionFactory _connectionFactory;

    public EventAttendeesRepository(IConnectionFactory connectionFactory) : base(connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    private IMongoDatabase GetConnection() => (MongoDB.Driver.IMongoDatabase)_connectionFactory.GetConnection;

    private string GetCollectionName() => string.Format("{0}Collection", typeof(EventAttendee).Name);

    public async Task<List<string>> JoinedMutualFriendImages(List<string> followingUserIds, string eventId)
    {
        var collection = GetConnection().GetCollection<EventAttendee>(GetCollectionName());

        var filter = Builders<EventAttendee>.Filter.Where(x => x.EventId == eventId && followingUserIds.Contains(x.User.Id));

        var projection = Builders<EventAttendee>.Projection.Include(x => x.User.ProfileImage);

        var result = await collection.Find(filter).ToListAsync();

        List<string> imageUrls = new List<string>();

        foreach (var item in result)
        {
            var profileImage = item?.User?.ProfileImage;
            if (!string.IsNullOrEmpty(profileImage))
            {
                imageUrls.Add(profileImage);
            }
        }
        return imageUrls;

    }
}