using AutoMapper;
using RestSharp;
using Topluluk.Services.EventAPI.Data.Interface;
using Topluluk.Services.EventAPI.Model.Dto;
using Topluluk.Services.EventAPI.Model.Dto.Http;
using Topluluk.Services.EventAPI.Model.Entity;
using Topluluk.Services.EventAPI.Services.Interface;
using Topluluk.Shared.Constants;
using Topluluk.Shared.Dtos;
using ResponseStatus = Topluluk.Shared.Enums.ResponseStatus;

namespace Topluluk.Services.EventAPI.Services.Implementation;

public class EventCommentService : IEventCommentService
{
    
    private readonly IMapper _mapper;
    private readonly RestClient _client;
    private readonly IEventCommentRepository _commentRepository;

    public EventCommentService( IMapper mapper, IEventCommentRepository commentRepository)
    {
        _mapper = mapper;
        _client = new RestClient();
        _commentRepository = commentRepository;
    }


    
    public async Task<Response<string>> CreateComment(string userId, CommentCreateDto dto)
    {
        try
        {
            EventComment comment = _mapper.Map<EventComment>(dto);
            comment.UserId = userId;
            comment.EventId = dto.EventId;
            DatabaseResponse response = await _commentRepository.InsertAsync(comment);
            return await Task.FromResult(Response<string>.Success(response.Data, ResponseStatus.Success));
        }
        catch (Exception e)
        {
            return await Task.FromResult(Response<string>.Fail($"Error occured {e}", ResponseStatus.InitialError));
        }

    }

    public async Task<Response<NoContent>> DeleteComment(string id, string eventId)
    {
        try
        {
            EventComment comment = await _commentRepository.GetFirstAsync(e => e.Id == eventId);

            if (comment == null)
            {
                return await Task.FromResult(Response<NoContent>.Fail("", ResponseStatus.NotFound));
            }
            
            if (!id.Equals(comment.UserId))
            {   
                return await Task.FromResult(Response<NoContent>.Fail("Unatuhorized",
                ResponseStatus.Unauthorized));
            }
            
            _commentRepository.DeleteByExpression(c => c.UserId == comment.UserId);
            return await Task.FromResult(Response<NoContent>.Success(ResponseStatus.Success));
        }
        catch (Exception e)
        {
            return await Task.FromResult(Response<NoContent>.Fail($"Some error occurred: {e}",
                ResponseStatus.InitialError));
        }
    }

    public async Task<Response<List<GetEventCommentDto>>> GetEventComments(string userId, string id, int skip = 0, int take = 10)
    {


        var response = _commentRepository.GetListByExpressionPaginated(skip,take, c => c.EventId == id);
        if (response != null && response.Count > 0)
        {
            List<GetEventCommentDto> dtos = _mapper.Map<List<EventComment>, List<GetEventCommentDto>>(response);
            IdList idList = new(dtos.Select(c => c.UserId).ToList());
            var userRequest = new RestRequest(ServiceConstants.API_GATEWAY + "/user/get-user-info-list").AddBody(idList);
            var userResponse = await _client.ExecutePostAsync<Response<List<UserInfoDto>>>(userRequest);
            if(!userResponse.IsSuccessful || userResponse.Data.Data == null)
            {
                throw new Exception("User service call exception");
            }
            foreach (var dto in dtos)
            {
                var user = userResponse.Data.Data.FirstOrDefault(u => u.Id == dto.UserId);
                if (user == null)
                {
                    dtos.Remove(dto);
                    continue;
                }
                dto.FirstName = user.FirstName;
                dto.LastName = user.LastName;
                dto.ProfileImage = user.ProfileImage;
                dto.Gender = user.Gender;
            }
            return Response<List<GetEventCommentDto>>.Success(dtos, ResponseStatus.Success);
        }
        return Response<List<GetEventCommentDto>>.Success(new(), ResponseStatus.Success);
    }

    public Task<Response<NoContent>> UpdateComment(string userId, CommentCreateDto dto)
    {
        throw new NotImplementedException();
    }
}