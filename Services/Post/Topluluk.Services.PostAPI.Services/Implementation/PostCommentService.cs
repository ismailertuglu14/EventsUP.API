using AutoMapper;
using Microsoft.AspNetCore.Http;
using RestSharp;
using Topluluk.Services.PostAPI.Data.Interface;
using Topluluk.Services.PostAPI.Model.Dto;
using Topluluk.Services.PostAPI.Model.Dto.Http;
using Topluluk.Services.PostAPI.Model.Entity;
using Topluluk.Services.PostAPI.Services.Interface;
using Topluluk.Shared.Constants;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Exceptions;
using Topluluk.Shared.Helper;
using ResponseStatus = Topluluk.Shared.Enums.ResponseStatus;

namespace Topluluk.Services.PostAPI.Services.Implementation;

public class PostCommentService : IPostCommentService
{
    private readonly IMapper _mapper;
    private readonly IPostRepository _postRepository;
    private readonly IPostCommentRepository _commentRepository;
    private readonly ICommentInteractionRepository _commentInteractionRepository;
    private readonly IHttpContextAccessor _httpContext;
    public PostCommentService(IMapper mapper, IPostRepository postRepository, IPostCommentRepository commentRepository, ICommentInteractionRepository commentInteractionRepository, IHttpContextAccessor httpContext)
    {
        _mapper = mapper;
        _postRepository = postRepository;
        _commentRepository = commentRepository;
        _commentInteractionRepository = commentInteractionRepository;
        _httpContext = httpContext;
    }
    private string Token => _httpContext.HttpContext.Request.Headers["Authorization"]!;
    public async Task<Response<List<CommentGetDto>>> GetComments(string userId, string postId, CommentFilter filter = CommentFilter.InteractionDescending, int skip = 0,int take = 10)
    {
        List<PostComment> response = new List<PostComment>();

        switch (filter)
        {
            case CommentFilter.InteractionDescending:
                response = await _commentRepository.GetPostCommentsDescendingByInteractionCount(skip, take, c => !c.IsDeleted && c.PostId == postId && (c.ParentCommentId == "" || c.ParentCommentId == null));
                break;
            case CommentFilter.InteractionAscending:
                response = await _commentRepository.GetPostCommentsAscendingByInteractionCount(skip, take, c => !c.IsDeleted && c.PostId == postId && (c.ParentCommentId == "" || c.ParentCommentId == null));
                break;
            case CommentFilter.TimeDescending:
                response = await _commentRepository.GetPostCommentsDescendingDate(skip, take, c => !c.IsDeleted && c.PostId == postId && (c.ParentCommentId == "" || c.ParentCommentId == null));
                break;
            case CommentFilter.TimeAscending:
                response = await _commentRepository.GetPostCommentsAscendingDate(skip, take, c => !c.IsDeleted && c.PostId == postId && (c.ParentCommentId == "" || c.ParentCommentId == null));
                break;
            default:
                response = await _commentRepository.GetPostCommentsDescendingByInteractionCount(skip, take, c => !c.IsDeleted && c.PostId == postId && (c.ParentCommentId == "" || c.ParentCommentId == null));
                break;
        }
        List<CommentGetDto> comments = _mapper.Map<List<PostComment>, List<CommentGetDto>>(response);
        List<string> commentIds = comments.Select(c => c.Id).ToList();
        var commentReplyCountTask =  _commentRepository.GetCommentsReplyCounts(commentIds);
        var commentsInteractedTask = _commentInteractionRepository.GetCommentsIsInteracted(userId, commentIds);
        var commentsInteractionCountsTask = _commentInteractionRepository.GetCommentsInteractionCounts(commentIds);
        await Task.WhenAll(commentsInteractedTask, commentReplyCountTask, commentsInteractionCountsTask);

        foreach (var comment in comments)
        {
            comment.ReplyCount = commentReplyCountTask.Result.TryGetValue(comment.Id, out int value) ? value : 0;
            comment.IsInteracted = commentsInteractedTask.Result.TryGetValue(comment.Id, out var isInteracted)
                ? isInteracted
                : new CommentInteracted();
            comment.InteractionCounts = commentsInteractionCountsTask.Result.TryGetValue(comment.Id, out var counts)
                ? counts
                : new CommentLikes();

            comment.IsEdited = response.Where(c => c.Id == comment.Id)!.FirstOrDefault()!.PreviousMessages != null ;
        }

        return Response<List<CommentGetDto>>.Success(comments,ResponseStatus.Success);
    }

    public async Task<Response<List<CommentGetDto>>> GetReplies(string commentId, int skip = 0, int take = 10)
    {
        List<PostComment> response =  await _commentRepository.GetPostCommentsDescendingDate(skip, take, c => !c.IsDeleted && c.ParentCommentId == commentId );
        if (response == null || response.Count == 0)
        {
            return Response<List<CommentGetDto>>.Success(new List<CommentGetDto>(),ResponseStatus.Success);
        }
        List<CommentGetDto> comments = _mapper.Map<List<PostComment>, List<CommentGetDto>>(response);
        List<string> commentIds = comments.Select(c => c.Id).ToList();
        foreach (var comment in comments)
        {
            comment.IsEdited = response!.Where(c => c.Id == comment.Id).FirstOrDefault().PreviousMessages != null ;
        }

        return Response<List<CommentGetDto>>.Success(comments,ResponseStatus.Success);
    }

    public async Task<Response<NoContent>> CreateComment(CommentCreateDto commentDto)
    {
        User? user = await HttpRequestHelper.GetUser(Token);
        if (user == null)
            throw new UnauthorizedAccessException();
        Post? post = await _postRepository.GetFirstAsync(p => p.Id == commentDto.PostId);
        if (post != null)
        {
            PostComment comment = _mapper.Map<PostComment>(commentDto);
            comment.User = user;
            await _commentRepository.InsertAsync(comment);
            return Response<NoContent>.Success( ResponseStatus.Success);
        }
        return Response<NoContent>.Fail("Post not found", ResponseStatus.NotFound);
    }

    public async Task<Response<NoContent>> UpdateComment(string userId, CommentUpdateDto commentDto)
    {
        User? user = await HttpRequestHelper.GetUser(userId);
        if (user == null)
            throw new UnauthorizedAccessException();
        PostComment? comment = await _commentRepository.GetFirstAsync(c => c.Id == commentDto.CommentId);

        if (comment == null)
            throw new NotFoundException("Comment Not Found");

        if (comment.User.Id != userId)
            throw new UnauthorizedAccessException();

        var previousMessage = new PreviousMessage()
        {
            Message = comment.Message,
            EditedDate = DateTime.Now
        };

        if (comment.PreviousMessages == null)
        {
            comment.PreviousMessages = new List<PreviousMessage>();
        }

        comment.PreviousMessages.Add(previousMessage);
        comment.Message = commentDto.Message;
        _commentRepository.Update(comment);

        return Response<NoContent>.Success(ResponseStatus.Success);
    }

    public async Task<Response<NoContent>> DeleteComment(string userId, string commentId)
    {
        PostComment comment = await _commentRepository.GetFirstAsync(c => c.Id == commentId);
        if (comment.User.Id != userId)
            return Response<NoContent>.Fail("UnAauthorized", ResponseStatus.NotAuthenticated);
        _commentRepository.DeleteById(commentId);
        return Response<NoContent>.Success(ResponseStatus.Success);

    }

    public async Task<Response<NoContent>> Interaction(string userId, string commentId, int type)
    {
        User? user = await HttpRequestHelper.GetUser(Token);
        if(user == null) throw new UnauthorizedAccessException();
        if (!Enum.IsDefined(typeof(CommentInteractionEnum), type))
            throw new ArgumentException("Interaction Type does not allowed! Please provide valid interaction type.");

        CommentInteraction commentInteraction = await _commentInteractionRepository.GetFirstAsync(c => c.CommentId == commentId && c.User.Id == userId);
        CommentInteraction interaction = new()
        {
            User = user,
            CommentId = commentId,
            Type = (CommentInteractionType)type
        };
        if (commentInteraction != null && (int)commentInteraction.Type != type)
        {
            _commentInteractionRepository.DeleteById(commentInteraction);
            await _commentInteractionRepository.InsertAsync(interaction);
        }
        else if (commentInteraction != null && (int)commentInteraction.Type == type)
        {
            _commentInteractionRepository.DeleteById(commentInteraction);
        }

        else if (commentInteraction == null && Enum.IsDefined(typeof(CommentInteractionEnum), type))
        {
            await _commentInteractionRepository.InsertAsync(interaction);
        }

        return Response<NoContent>.Success(ResponseStatus.Success);
    }

    public async Task<Response<List<string>>> GetCommentFilterParameters()
    {
        return Response<List<string>>.Success(Enum.GetNames(typeof(CommentFilter)).ToList(), ResponseStatus.Success);
    }
}