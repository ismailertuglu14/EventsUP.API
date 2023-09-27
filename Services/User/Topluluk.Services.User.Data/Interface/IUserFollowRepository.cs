using DBHelper.Repository;
using System.Linq.Expressions;
using Topluluk.Services.User.Model.Entity;

namespace Topluluk.Services.User.Data.Interface;

public interface IUserFollowRepository : IGenericRepository<UserFollow>
{
    Task<List<string>?> GetFollowingIds(int skip, int take, Expression<Func<UserFollow, bool>> expression);

    Task<List<string>?> GetFollowerIds(int skip, int take, Expression<Func<UserFollow, bool>> expression);
}