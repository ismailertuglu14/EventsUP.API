using System;
using System.Linq.Expressions;
using DBHelper.Repository;
using DBHelper.Repository.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Topluluk.Services.PostAPI.Model.Dto;
using Topluluk.Services.PostAPI.Model.Entity;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Messages.User;

namespace Topluluk.Services.PostAPI.Data.Interface
{
	public interface IPostRepository : IGenericRepository<Post>
	{
		Task<GetPostByIdDto> GetPostById(string id);
		Task<bool> DeletePosts(string userId);
		Task<List<GetPostForFeedDto>> GetPostsWithDescending(string sourceUserId, int skip, int take, Expression<Func<Post, bool>> expression);
		Task<bool> UserUpdated(User newUser);

	}
}

