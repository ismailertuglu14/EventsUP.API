using Microsoft.AspNetCore.Http;
using Topluluk.Shared.Dtos;

namespace Topluluk.Shared.BaseModels
{
    public interface IBaseService
    {
        IHttpContextAccessor _httpContextAccessor { get; set; }
        
        /// <summary>
        /// This token is taken from header of request.
        /// </summary>
        string Token { get; }

        /// <summary>
        /// This function returns current user with Token in header of request.
        /// </summary>
        /// <returns></returns>
        Task<User?> GetCurrentUserAsync();
    }
}
