using AutoMapper;
using Microsoft.AspNetCore.Http;
using RestSharp;
using Topluluk.Services.EventAPI.Data.Interface;
using Topluluk.Services.EventAPI.Model.Dto;
using Topluluk.Services.EventAPI.Model.Entity;
using Topluluk.Services.EventAPI.Services.Interface;
using Topluluk.Shared.BaseModels;
using Topluluk.Shared.Dtos;
using ResponseStatus = Topluluk.Shared.Enums.ResponseStatus;

namespace Topluluk.Services.EventAPI.Services.Implementation;

public class EventCommentService : BaseService, IEventCommentService
{
    
    private readonly IMapper _mapper;
    private readonly RestClient _client;
    private readonly IEventCommentRepository _commentRepository;
    public EventCommentService(IMapper mapper, IEventCommentRepository commentRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
        _mapper = mapper;
        _client = new RestClient();
        _commentRepository = commentRepository;
    }

    public async Task<Response<string>> CreateComment(string userId, CommentCreateDto dto)
    {
        try
        {
            User? user = await GetCurrentUserAsync();
            if (user == null) throw new UnauthorizedAccessException();

            EventComment comment = _mapper.Map<EventComment>(dto);
            comment.User = user;
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
            
            if (!id.Equals(comment.User.Id))
            {   
                return await Task.FromResult(Response<NoContent>.Fail("Unatuhorized",
                ResponseStatus.Unauthorized));
            }
            
            _commentRepository.DeleteByExpression(c => c.User.Id == comment.User.Id);
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
        List<EventComment> response = _commentRepository.GetListByExpressionPaginated(skip,take, c => c.EventId == id);
        if (response != null && response.Count > 0)
        {
            List<GetEventCommentDto> dtos = _mapper.Map<List<EventComment>, List<GetEventCommentDto>>(response);
            return Response<List<GetEventCommentDto>>.Success(dtos, ResponseStatus.Success);
        }
        return Response<List<GetEventCommentDto>>.Success(new(), ResponseStatus.Success);
    }

    public Task<Response<NoContent>> UpdateComment(string userId, CommentCreateDto dto)
    {
        throw new NotImplementedException();
    }
}