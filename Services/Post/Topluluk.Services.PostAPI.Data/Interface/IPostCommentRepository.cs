using System;
using System.Linq.Expressions;
using DBHelper.Repository;
using Topluluk.Services.PostAPI.Model.Entity;
using Topluluk.Shared.Dtos;

namespace Topluluk.Services.PostAPI.Data.Interface
{
	public interface IPostCommentRepository : IGenericRepository<PostComment>
	{
        Task<List<PostComment>> GetPostCommentsAscendingDate(int skip, int take, Expression<Func<PostComment, bool>> predicate);
        Task<List<PostComment>> GetPostCommentsDescendingDate(int skip, int take, Expression<Func<PostComment,bool>> predicate);
        Task<List<PostComment>> GetPostCommentsAscendingByInteractionCount(int skip, int take, Expression<Func<PostComment, bool>> predicate);

        Task<List<PostComment>> GetPostCommentsDescendingByInteractionCount(int skip, int take, Expression<Func<PostComment, bool>> predicate);
		Task<bool> DeletePostsComments(string userId);
		Task<Dictionary<string, int>> GetPostCommentCounts(List<string> postIds);
		Task<Dictionary<string, int>> GetCommentsReplyCounts(List<string> commentIds);
        Task<bool> UserUpdated(User newUser);

    }
}

