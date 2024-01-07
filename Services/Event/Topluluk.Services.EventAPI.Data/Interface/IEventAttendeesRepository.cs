using DBHelper.Repository;
using Topluluk.Services.EventAPI.Model.Entity;

namespace Topluluk.Services.EventAPI.Data.Interface;

public interface IEventAttendeesRepository : IGenericRepository<EventAttendee>
{
    Task<List<string>> JoinedMutualFriendImages(List<string> followingUserIds, string eventId);
}