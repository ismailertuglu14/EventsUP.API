using Microsoft.AspNetCore.Http;
using Topluluk.Shared.Dtos;

namespace Topluluk.Shared.BaseModels
{
    public interface IBaseService
    {
        IHttpContextAccessor _httpContextAccessor { get; set; }
        string Token { get; }
        Task<User?> GetCurrentUserAsync();
    }
}
