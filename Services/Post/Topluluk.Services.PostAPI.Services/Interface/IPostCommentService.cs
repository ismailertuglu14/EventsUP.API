using Topluluk.Services.PostAPI.Model.Dto;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.PostAPI.Services.Interface;

public interface IPostCommentService
{
    Task<Response<List<CommentGetDto>>> GetComments(string userId, string postId, CommentFilter filter = CommentFilter.InteractionDescending, int skip = 0, int take = 10);
    Task<Response<List<CommentGetDto>>> GetReplies(string commentId, int skip = 0, int take = 10);
    Task<Response<CommentGetDto>> CreateComment(CommentCreateDto commentDto);
    Task<Response<NoContent>> UpdateComment(string userId, CommentUpdateDto commentDto);
    Task<Response<NoContent>> DeleteComment(string userId, string commentId);
    Task<Response<NoContent>> Interaction(string userId, string commentId, int type);
    Task<Response<List<string>>> GetCommentFilterParameters();


}