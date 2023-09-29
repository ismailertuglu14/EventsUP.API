using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using Topluluk.Services.CommunityAPI.Data.Interface;
using Topluluk.Services.CommunityAPI.Model.Entity;
using Topluluk.Services.CommunityAPI.Services.Interface;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Exceptions;
using Topluluk.Shared.Helper;
using ResponseStatus = Topluluk.Shared.Enums.ResponseStatus;

namespace Topluluk.Services.CommunityAPI.Services.Implementation;

public class ParticipiantService : IParticipiantService
{

    private readonly ICommunityRepository _communityRepository;
    private readonly ICommunityParticipiantRepository _participiantRepository;
    private readonly IMapper _mapper;
    private readonly RestClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public ParticipiantService(ICommunityParticipiantRepository participiantRepository, ICommunityRepository communityRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _participiantRepository = participiantRepository;
        _communityRepository = communityRepository;
        _mapper = mapper;
        _client = new RestClient();
        _httpContextAccessor = httpContextAccessor;
    }
    private string? Token => _httpContextAccessor.HttpContext.Request.Headers["Authorization"];
    public async Task<Response<string>> Join(string userId, string token, string communityId)
    {
        Community community = await _communityRepository.GetFirstAsync(c => c.Id == communityId);
        List<CommunityParticipiant>? participiants =  _participiantRepository.GetListByExpression(p => p.User.Id == userId && p.CommunityId == community.Id);

        if (participiants.Any(p => p.User.Id == userId))
        {
            return await Task.FromResult(Response<string>.Success("You have already participiants this community", ResponseStatus.Success));
        }

        if (community.IsRestricted && !community.IsVisible && community.BlockedUsers.Contains(userId))
        {
            return await Task.FromResult(Response<string>.Fail("Not found community", ResponseStatus.NotFound));
        }

        if (participiants.Count >= community.ParticipiantLimit)
        {
            return await Task.FromResult(Response<string>.Fail("Community full now!", ResponseStatus.Failed));
        }

        User? user = await HttpRequestHelper.GetUser(Token);
        if (user == null) throw new UnauthorizedAccessException();

        CommunityParticipiant participiant = new()
        {
            User = user,
            CommunityId = communityId,
        };

        if (!community.IsPublic)
        {
            var isRequested = await _participiantRepository.AnyAsync(c => !c.IsDeleted
                                                                && c.CommunityId == communityId && c.User.Id == userId
                                                                && c.Status != ParticipiantStatus.ACCEPTED);
            if (!isRequested)
            {
                participiant.Status = ParticipiantStatus.REQUESTED;
                await _participiantRepository.InsertAsync(participiant);
                return await Task.FromResult(Response<string>.Success("Send request", ResponseStatus.Success));
            }

            return await Task.FromResult(Response<string>.Success("You already send request this community", ResponseStatus.Success));
        }


        participiant.Status = ParticipiantStatus.ACCEPTED;
        await _participiantRepository.InsertAsync(participiant);

        return await Task.FromResult(Response<string>.Success("Joined", ResponseStatus.Success));

    }

    /// <summary>
    /// Admin can not leave community. If you want to leave community, you must assign new admin.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="token"></param>
    /// <param name="communityId"></param>
    /// <returns></returns>
    public async Task<Response<NoContent>> Leave(string userId, string token, string communityId)
    {
        try
        {
            if (userId.IsNullOrEmpty())
            {
                return await Task.FromResult(Response<NoContent>.Fail("", ResponseStatus.BadRequest));
            }

            Community? community = await _communityRepository.GetFirstAsync(c => c.Id == communityId);

            if (community == null)
            {
                return await Task.FromResult(Response<NoContent>.Fail("Community Not Found", ResponseStatus.NotFound));
            }

            if (community.Admin.Id == userId)
            {
                return await Task.FromResult(Response<NoContent>.Fail("You are the owner of this community!", ResponseStatus.CommunityOwnerExist));
            }

            CommunityParticipiant? participiant = await _participiantRepository.GetFirstAsync(p => p.User.Id == userId && p.CommunityId == communityId);
            if (participiant != null)
            {
                _participiantRepository.DeleteCompletely(participiant.Id);
                return await Task.FromResult(Response<NoContent>.Success(ResponseStatus.Success));
            }

            return await Task.FromResult(
                Response<NoContent>.Fail("You are not participiant", ResponseStatus.Failed));
        }
        catch (Exception e)
        {
            return await Task.FromResult(Response<NoContent>.Fail($"Some error occurred : {e}", ResponseStatus.InitialError));
        }
    }
    public async Task<Response<NoContent>> AcceptUserJoinRequest(string userId, string communityId, string targetUserId)
    {
        Community community = await _communityRepository.GetFirstAsync(c => c.Id == communityId);

        if (community.Admin.Id != userId)
            throw new UnauthorizedAccessException();

        CommunityParticipiant participiantRequest = await _participiantRepository.GetFirstAsync(p =>
            p.CommunityId == communityId && p.User.Id == targetUserId && p.Status == ParticipiantStatus.REQUESTED);

        if (participiantRequest == null)
            throw new ArgumentNullException();

        participiantRequest.Status = ParticipiantStatus.ACCEPTED;
        _participiantRepository.Update(participiantRequest);
        return Response<NoContent>.Success(ResponseStatus.Success);
    }
    public async Task<Response<NoContent>> DeclineUserJoinRequest(string userId, string communityId, string targetUserId)
    {
        Community community = await _communityRepository.GetFirstAsync(c => c.Id == communityId);

        if (community.Admin.Id != userId)
            throw new UnauthorizedAccessException();

        CommunityParticipiant participiantRequest = await _participiantRepository.GetFirstAsync(p =>
            p.CommunityId == communityId && p.User.Id == targetUserId && p.Status == ParticipiantStatus.REQUESTED);

        if (participiantRequest == null)
            throw new ArgumentNullException();

        participiantRequest.Status = ParticipiantStatus.REJECTED;
        _participiantRepository.Update(participiantRequest);
        return Response<NoContent>.Success(ResponseStatus.Success);
    }
    public async Task<Response<List<User>>> GetJoinRequests(string userId, string communityId, int skip = 0, int take = 10)
    {
        Community? community = await _communityRepository.GetFirstAsync(c => c.Id == communityId);

        if (community == null)
            throw new NotFoundException();

        if (community.Admin.Id != userId)
            throw new UnauthorizedAccessException();

        var joinRequests = _participiantRepository.GetListByExpressionPaginated(skip, take,
            p => p.CommunityId == communityId && p.Status == ParticipiantStatus.REQUESTED);

        var userDtos = new List<User>();
        foreach (var joinRequest in joinRequests)
        {
            userDtos.Add(joinRequest.User);
        }


        return Response<List<User>>.Success(userDtos, ResponseStatus.Success);
    }
    public async Task<Response<List<User>>> GetParticipiants(string token, string id, int skip = 0, int take = 10)
    {
        Community? community = await _communityRepository.GetFirstAsync(c => c.Id == id);

        if (community == null)
            throw new NotFoundException("Community not found");

        List<CommunityParticipiant>? participiants = _participiantRepository.GetListByExpressionPaginated(skip, take, c => c.CommunityId == id && c.User.Id != community.Admin.Id && c.Status == ParticipiantStatus.ACCEPTED);
        var userDtos = new List<User>();
        foreach (var participiant in participiants)
        {
            userDtos.Add(participiant.User);
        }
        return Response<List<User>>.Success(userDtos, ResponseStatus.Success);
    }

    public async Task<Response<List<User>>> SearchParticipiant(string communityId, string query, int skip = 0, int take = 10)
    {
        List<CommunityParticipiant> participiants = await _participiantRepository.GetListByExpressionAsync(p => p.CommunityId == communityId);

        if (participiants == null || participiants.Count == 0)
        {
            return Response<List<User>>.Success(new List<User>(), ResponseStatus.Success);
        }
        var filteredUsers = participiants.Where(u =>
                                 (u.User.FullName.ToLower()).Replace(" ", "").Contains(query.Replace(" ", "").ToLower(), StringComparison.OrdinalIgnoreCase)).ToList();

        List<User> dtos = _mapper.Map<List<User>>(filteredUsers);
        return Response<List<User>>.Success(dtos, ResponseStatus.Success);
    }

}