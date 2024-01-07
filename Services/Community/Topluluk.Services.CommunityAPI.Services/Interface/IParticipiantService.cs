using Topluluk.Services.CommunityAPI.Model.Dto.Http;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.CommunityAPI.Services.Interface;

public interface IParticipiantService
{
    Task<Response<string>> Join(string userId, string token, string communityId);

    /// <summary>
    /// Admin can not leave community. If you want to leave community, you must assign new admin.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="token"></param>
    /// <param name="communityId"></param>
    /// <returns></returns>
    Task<Response<NoContent>> Leave(string userId, string token, string communityId);
    Task<Response<NoContent>> AcceptUserJoinRequest(string userId, string communityId, string targetUserId);
    Task<Response<NoContent>> DeclineUserJoinRequest(string userId, string communityId, string targetUserId);
    Task<Response<List<User>>> GetJoinRequests(string userId, string communityId, int skip = 0, int take = 10);
    Task<Response<List<User>>> GetParticipiants(string token, string id, int skip = 0, int take = 10);
    Task<Response<List<User>>> SearchParticipiant(string communityId, string name, int skip = 0, int take = 10);

}