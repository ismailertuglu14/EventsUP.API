using AutoMapper;
using Microsoft.AspNetCore.Http;
using RestSharp;
using Topluluk.Services.EventAPI.Data.Interface;
using Topluluk.Services.EventAPI.Model.Dto;
using Topluluk.Services.EventAPI.Model.Dto.Http;
using Topluluk.Services.EventAPI.Model.Entity;
using Topluluk.Services.EventAPI.Services.Interface;
using Topluluk.Shared.Constants;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Helper;
using ResponseStatus = Topluluk.Shared.Enums.ResponseStatus;

namespace Topluluk.Services.EventAPI.Services.Implementation;

public class EventCommentService : IEventCommentService
{
    
    private readonly IMapper _mapper;
    private readonly RestClient _client;
    private readonly IEventCommentRepository _commentRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public EventCommentService(IMapper mapper, IEventCommentRepository commentRepository, IHttpContextAccessor httpContextAccessor)
    {
        _mapper = mapper;
        _client = new RestClient();
        _commentRepository = commentRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    private string Token => _httpContextAccessor.HttpContext.Request.Headers["Authorization"];


    public async Task<Response<string>> CreateComment(string userId, CommentCreateDto dto)
    {
        try
        {
            User? user = await HttpRequestHelper.GetUser(Token);
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